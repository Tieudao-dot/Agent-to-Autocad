using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ClaudeCAD.Plugin.Core;

namespace ClaudeCAD.Plugin.Drawing
{
    /// <summary>
    /// verify_drawing (docs/ke-hoach-tu-kiem.md): measures what the turn drew with VerifyRules and repairs the
    /// safe mistakes (auto_fix) inside the turn's undo group. The checking itself never changes anything.
    /// </summary>
    public static partial class DrawingService
    {
        public const string VerifyTool = "verify_drawing";
        private const int MaxVerifyTargets = 5000;
        private const int NeighbourScanMs = 3000;

        /// <summary>
        /// auto_fix makes the call a write only when it can repair something: the turn changed the drawing and
        /// "Chỉ xem" is off (read-only: the check still runs, nothing is repaired).
        /// </summary>
        public static bool VerifyWrites(Params p, ITurnHost host) =>
            p.Bool("auto_fix", true) && !host.ReadOnly && host.TurnChanges().Count > 0;

        private static void RegisterVerify(ToolDispatcher d, ITurnHost host)
        {
            d.Register(new ToolDef("verify_drawing", false, (ctx, p) => Verify(ctx, p, host), batchForbidden: true));
        }

        private static object Verify(ToolContext ctx, Params p, ITurnHost host)
        {
            var turn = host.TurnChanges();
            var explicitHandles = p.Has("handles") ? p.Handles("handles", MaxVerifyTargets) : null;
            var targets = explicitHandles ?? turn.Keys.ToList();
            var std = host.Standard;
            var opts = new VOptions
            {
                // gap: the user's own value > the skill's "Ngưỡng khe hở" > 10 mm
                Gap = Math.Max(0.1, Math.Min(1000, p.Num("gap", std?.GapMm ?? VerifyRules.DefaultGap))),
                Standard = std != null && !std.IsEmpty ? std : null,
                Scale = host.PrintScale,
                LayerAllowed = host.UserAllowsLayer,
            };
            var expect = p.ObjOpt("expect");
            if (expect != null)
            {
                opts.Expect = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var k in expect.Keys) opts.Expect[k] = expect.Int(k, 0, 0, 1000000);
            }
            bool autoFix = p.Bool("auto_fix", true);

            if (targets.Count == 0)
            {
                var none = new Dictionary<string, object>
                {
                    ["checked"] = 0, ["ok"] = true, ["issues"] = new List<object>(),
                    ["note"] = "This turn has not drawn or changed anything yet: nothing to check. Pass handles to check existing objects.",
                };
                return none;
            }

            var sw = Stopwatch.StartNew();
            bool truncated = false;
            var targetShapes = LoadTargets(ctx, targets.Take(MaxVerifyTargets), turn, explicitHandles != null, out var owners);
            var neighbours = LoadNeighbours(ctx, targetShapes, owners, opts.Gap, sw, ref truncated);
            var shapes = targetShapes.Concat(neighbours).ToList();
            var rep = VerifyRules.Check(shapes, opts);

            // auto_fix: only objects of this turn are ever repaired, never the user's existing ones
            var fixedIssues = new List<VIssue>();
            var fixNotes = new List<string>();
            if (autoFix && ctx.Writable)
            {
                var fixes = VerifyRules.SelectFixes(rep).Where(f => turn.ContainsKey(f.Handle)).ToList();
                var applied = new HashSet<VFix>();
                foreach (var f in fixes)
                {
                    try
                    {
                        if (ApplyFix(ctx, f)) applied.Add(f);
                    }
                    catch (ToolException te) { fixNotes.Add($"{f.Handle}: {te.Message}"); }
                    catch (Autodesk.AutoCAD.Runtime.Exception ae) { fixNotes.Add($"{f.Handle}: AutoCAD {ae.ErrorStatus}"); }
                }
                if (applied.Count > 0)
                {
                    fixedIssues = rep.Issues.Where(i => i.Fix != null && applied.Contains(i.Fix)).ToList();
                    var alive = targets.Where(h => !applied.Any(f => f.Kind == "erase" && string.Equals(f.Handle, h, StringComparison.OrdinalIgnoreCase)));
                    targetShapes = LoadTargets(ctx, alive.Take(MaxVerifyTargets), turn, explicitHandles != null, out _);
                    rep = VerifyRules.Check(targetShapes.Concat(neighbours).ToList(), opts);
                }
            }
            else if (autoFix && host.ReadOnly)
            {
                fixNotes.Add("auto_fix skipped: \"Chỉ xem\" is on, nothing was changed.");
            }
            else if (autoFix && turn.Count == 0)
            {
                fixNotes.Add("auto_fix skipped: these objects were not drawn or changed by this turn (the user's existing objects are never repaired automatically).");
            }

            var result = new Dictionary<string, object>
            {
                ["checked"] = rep.Checked,
                ["ok"] = rep.Total == 0,
                ["gap_mm"] = opts.Gap,
                ["issues"] = rep.Issues.Select(i => (object)IssueDict(i, autoFix && ctx.Writable)).ToList(),
            };
            if (rep.Omitted.Count > 0)
                result["more"] = rep.Omitted.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
            if (fixedIssues.Count > 0)
                result["fixed"] = fixedIssues.Select(i => (object)new Dictionary<string, object>
                {
                    ["code"] = i.Code, ["message"] = i.Message, ["objects"] = i.Handles.Cast<object>().ToList(),
                }).ToList();
            if (fixNotes.Count > 0) result["fix_notes"] = fixNotes.Cast<object>().ToList();
            if (truncated) result["note"] = "The drawing is large: only part of the neighbouring objects was compared.";
            if (rep.Total > 0)
                result["next"] = "Fix each remaining issue with the normal tools (or explain in the reply why it is intentional and pass its code in 'ignore'), then call verify_drawing again.";

            host.RecordSelfCheck(SelfCheckSummary.From(rep, fixedIssues.Count));
            return result;
        }

        private static Dictionary<string, object> IssueDict(VIssue i, bool fixing)
        {
            var d = new Dictionary<string, object>
            {
                ["code"] = i.Code,
                // not "handles": the assistant reads that key as "objects this call created"
                ["objects"] = i.Handles.Cast<object>().ToList(),
                ["message"] = i.Message,
                ["fix"] = i.Hint,
            };
            if (i.At != null) d["at"] = new List<object> { Acad.R(i.At[0]), Acad.R(i.At[1]) };
            if (i.Fix != null && !fixing) d["auto_fixable"] = true;
            return d;
        }

        // ------------------------------------------------------------------ reading

        private static List<VShape> LoadTargets(ToolContext ctx, IEnumerable<string> handles, IReadOnlyDictionary<string, string> turn, bool explicitList, out HashSet<ObjectId> owners)
        {
            owners = new HashSet<ObjectId>();
            var list = new List<VShape>();
            var layoutOwner = new Dictionary<ObjectId, bool>();
            foreach (var h in handles)
            {
                ObjectId id;
                try { id = Acad.IdFromHandle(ctx.Db, h); }
                catch (ToolException) { if (explicitList) throw; continue; } // erased later in the turn
                if (!(ctx.Tr.GetObject(id, OpenMode.ForRead, false) is Entity e) || e.IsErased) continue;
                if (!layoutOwner.TryGetValue(e.OwnerId, out var isLayout))
                {
                    isLayout = ctx.Tr.GetObject(e.OwnerId, OpenMode.ForRead) is BlockTableRecord btr && btr.IsLayout;
                    layoutOwner[e.OwnerId] = isLayout;
                }
                if (!isLayout) continue; // entities inside block definitions are not drawn objects
                owners.Add(e.OwnerId);
                var s = ToShape(e);
                s.OfTurn = true;
                s.IsNew = !turn.TryGetValue(s.Handle, out var kind) ? explicitList : kind == "added";
                list.Add(s);
            }
            return list;
        }

        private static readonly HashSet<string> NeighbourClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LINE", "LWPOLYLINE", "ARC", "CIRCLE", "TEXT", "MTEXT", "DIMENSION",
        };

        /// <summary>Objects around the checked ones (what they may touch, overlap or duplicate).</summary>
        private static List<VShape> LoadNeighbours(ToolContext ctx, List<VShape> targets, HashSet<ObjectId> owners, double gap, Stopwatch sw, ref bool truncated)
        {
            var r = new List<VShape>();
            var boxes = targets.Where(t => t.Min != null && t.Max != null).ToList();
            if (boxes.Count == 0) return r;
            double pad = Math.Max(gap * 2, 1);
            var inTargets = new HashSet<string>(targets.Select(t => t.Handle), StringComparer.OrdinalIgnoreCase);
            double x0 = boxes.Min(b => b.Min[0]) - pad, y0 = boxes.Min(b => b.Min[1]) - pad;
            double x1 = boxes.Max(b => b.Max[0]) + pad, y1 = boxes.Max(b => b.Max[1]) + pad;
            foreach (var owner in owners)
            {
                var btr = (BlockTableRecord)ctx.Tr.GetObject(owner, OpenMode.ForRead);
                foreach (ObjectId id in btr)
                {
                    if (sw.ElapsedMilliseconds > NeighbourScanMs) { truncated = true; return r; }
                    string dxf;
                    try { dxf = id.ObjectClass.DxfName; } catch { continue; }
                    if (!NeighbourClasses.Contains(dxf)) continue;
                    if (inTargets.Contains(id.Handle.ToString())) continue;
                    if (!(ctx.Tr.GetObject(id, OpenMode.ForRead, false) is Entity e) || e.IsErased) continue;
                    var ext = EntityInfo.Extents(e);
                    if (!ext.HasValue) continue;
                    var mn = ext.Value.MinPoint;
                    var mx = ext.Value.MaxPoint;
                    if (mx.X < x0 || mn.X > x1 || mx.Y < y0 || mn.Y > y1) continue;
                    // only near one of the checked objects, not just inside the overall window
                    bool near = boxes.Any(b => mx.X >= b.Min[0] - pad && mn.X <= b.Max[0] + pad && mx.Y >= b.Min[1] - pad && mn.Y <= b.Max[1] + pad);
                    if (!near) continue;
                    r.Add(ToShape(e));
                }
            }
            return r;
        }

        private static double[] P2(Point3d p) => new[] { p.X, p.Y };

        private static bool Flat(Vector3d normal) => normal.IsParallelTo(Vector3d.ZAxis) && normal.Z > 0;

        internal static VShape ToShape(Entity e)
        {
            var s = new VShape { Handle = e.Handle.ToString(), Layer = e.Layer, Type = "OTHER" };
            var ext = EntityInfo.Extents(e);
            if (ext.HasValue) { s.Min = P2(ext.Value.MinPoint); s.Max = P2(ext.Value.MaxPoint); }
            try
            {
                s.ColorOverride = !e.Color.IsByLayer;
                s.LinetypeOverride = !string.Equals(e.Linetype, "BYLAYER", StringComparison.OrdinalIgnoreCase);
                s.LineweightOverride = e.LineWeight != LineWeight.ByLayer;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception) { }
            switch (e)
            {
                case Line l:
                    s.Type = "LINE";
                    s.Pts.Add(P2(l.StartPoint));
                    s.Pts.Add(P2(l.EndPoint));
                    break;
                case Polyline pl when Flat(pl.Normal):
                    s.Type = "POLY";
                    s.Closed = pl.Closed;
                    s.Bulges = new List<double>();
                    for (int i = 0; i < pl.NumberOfVertices; i++)
                    {
                        var v = pl.GetPoint2dAt(i);
                        s.Pts.Add(new[] { v.X, v.Y });
                        s.Bulges.Add(pl.GetBulgeAt(i));
                    }
                    break;
                case Arc a when Flat(a.Normal):
                    s.Type = "ARC";
                    s.Center = P2(a.Center);
                    s.Radius = a.Radius;
                    s.StartAngle = a.StartAngle;
                    s.EndAngle = a.EndAngle;
                    s.Pts.Add(P2(a.StartPoint));
                    s.Pts.Add(P2(a.EndPoint));
                    break;
                case Circle c:
                    s.Type = "CIRCLE";
                    s.Center = P2(c.Center);
                    s.Radius = c.Radius;
                    break;
                case DBText t:
                    s.Type = "TEXT";
                    s.Text = t.TextString;
                    s.Box = TextBox(t, ext);
                    s.Insert = P2(t.Justify == AttachmentPoint.BaseLeft ? t.Position : t.AlignmentPoint);
                    s.TextHeight = t.Height;
                    break;
                case MText m:
                    s.Type = "TEXT";
                    s.Text = m.Text;
                    s.Box = MTextBox(m, ext);
                    s.Insert = P2(m.Location);
                    s.TextHeight = m.TextHeight;
                    break;
                case BlockReference br:
                    s.Insert = P2(br.Position);
                    break;
                case DBPoint pt:
                    s.Insert = P2(pt.Position);
                    break;
                case Dimension d:
                    s.Type = "DIM";
                    s.Box = DimTextBox(d);
                    try { s.DimScale = d.Dimscale; s.DimTextHeight = d.Dimtxt; } catch (Autodesk.AutoCAD.Runtime.Exception) { }
                    break;
                case Hatch _:
                    s.Type = "HATCH";
                    break;
            }
            return s;
        }

        private static List<double[]> Rect(double[] origin, double w, double h, double rot)
        {
            double c = Math.Cos(rot), sn = Math.Sin(rot);
            double[] At(double u, double v) => new[] { origin[0] + u * c - v * sn, origin[1] + u * sn + v * c };
            return new List<double[]> { At(0, 0), At(w, 0), At(w, h), At(0, h) };
        }

        private static List<double[]> AxisBox(Extents3d? ext) =>
            ext.HasValue ? new List<double[]>
            {
                new[] { ext.Value.MinPoint.X, ext.Value.MinPoint.Y }, new[] { ext.Value.MaxPoint.X, ext.Value.MinPoint.Y },
                new[] { ext.Value.MaxPoint.X, ext.Value.MaxPoint.Y }, new[] { ext.Value.MinPoint.X, ext.Value.MaxPoint.Y },
            } : null;

        /// <summary>Rotated box of a single-line text: from its left baseline point, its height and its width.</summary>
        private static List<double[]> TextBox(DBText t, Extents3d? ext)
        {
            if (!ext.HasValue) return null;
            double rot = t.Rotation, h = t.Height;
            if (Math.Abs(Math.Sin(rot)) < 1e-6 && Math.Cos(rot) > 0) return AxisBox(ext);
            double w = ext.Value.MaxPoint.X - ext.Value.MinPoint.X, hh = ext.Value.MaxPoint.Y - ext.Value.MinPoint.Y;
            double c = Math.Abs(Math.Cos(rot)), sn = Math.Abs(Math.Sin(rot));
            double width = c > 0.3 ? (w - h * sn) / c : (hh - h * c) / sn;
            if (width <= 0) return AxisBox(ext);
            return Rect(P2(t.Position), width, h, rot);
        }

        private static List<double[]> MTextBox(MText m, Extents3d? ext)
        {
            try
            {
                var pts = m.GetBoundingPoints();
                if (pts != null && pts.Count == 4)
                {
                    // the 4 corners as a ring, whatever order AutoCAD returns them in
                    var ring = new List<double[]>();
                    foreach (Point3d q in pts) ring.Add(P2(q));
                    double cx = ring.Average(q => q[0]), cy = ring.Average(q => q[1]);
                    return ring.OrderBy(q => Math.Atan2(q[1] - cy, q[0] - cx)).ToList();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception) { }
            return AxisBox(ext);
        }

        /// <summary>Approximate box of a dimension's text, centred on its text position.</summary>
        private static List<double[]> DimTextBox(Dimension d)
        {
            try
            {
                double h = d.Dimtxt * (d.Dimscale > 0 ? d.Dimscale : 1);
                if (h <= 0) return null;
                string text = d.DimensionText;
                if (string.IsNullOrEmpty(text) || text.Contains("<>"))
                {
                    string m = d.Measurement.ToString("F" + Math.Max(0, Math.Min(8, d.Dimdec)), CultureInfo.InvariantCulture);
                    text = string.IsNullOrEmpty(text) ? m : text.Replace("<>", m);
                }
                double w = Math.Max(1, text.Length) * h * 0.7;
                double rot = d.TextRotation;
                if (Math.Abs(rot) < 1e-9)
                {
                    if (d is RotatedDimension rd) rot = rd.Rotation;
                    else if (d is AlignedDimension ad) rot = Math.Atan2(ad.XLine2Point.Y - ad.XLine1Point.Y, ad.XLine2Point.X - ad.XLine1Point.X);
                }
                // texts read from below or from the right
                double n = Geometry.NormalizeRad(rot);
                if (n > Math.PI / 2 + 1e-9 && n <= 3 * Math.PI / 2 + 1e-9) rot += Math.PI;
                var c = d.TextPosition;
                double cs = Math.Cos(rot), sn = Math.Sin(rot);
                var origin = new[] { c.X - (w / 2) * cs + (h / 2) * sn, c.Y - (w / 2) * sn - (h / 2) * cs };
                return Rect(origin, w, h, rot);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception) { return null; }
        }

        // ------------------------------------------------------------------ repairing

        private static bool ApplyFix(ToolContext ctx, VFix f)
        {
            var e = Acad.GetEntity(ctx, f.Handle, true);
            switch (f.Kind)
            {
                case "erase":
                    e.Erase();
                    return true;
                case "close":
                    if (!(e is Polyline pl) || pl.NumberOfVertices < 4) return false;
                    pl.RemoveVertexAt(pl.NumberOfVertices - 1);
                    pl.Closed = true;
                    return true;
                case "move_vertex":
                    if (e is Line l)
                    {
                        if (f.Vertex == 0) l.StartPoint = new Point3d(f.To[0], f.To[1], l.StartPoint.Z);
                        else l.EndPoint = new Point3d(f.To[0], f.To[1], l.EndPoint.Z);
                        return true;
                    }
                    if (e is Polyline p2 && f.Vertex >= 0 && f.Vertex < p2.NumberOfVertices)
                    {
                        p2.SetPointAt(f.Vertex, new Point2d(f.To[0], f.To[1]));
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}
