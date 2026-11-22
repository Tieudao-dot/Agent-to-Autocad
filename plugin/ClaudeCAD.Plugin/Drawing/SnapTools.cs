using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ClaudeCAD.Plugin.Core;

namespace ClaudeCAD.Plugin.Drawing
{
    /// <summary>
    /// Resolves point references of one tool call against the drawing (docs/ke-hoach-tu-kiem.md §9, layer A).
    /// "@n" / "@n:k" = the object created by call n (0-based, as in the batch results) of the running batch_execute.
    /// </summary>
    public sealed class SnapResolver : ISnapResolver
    {
        private readonly ToolContext _ctx;
        private int _depth;

        public SnapResolver(ToolContext ctx) => _ctx = ctx;

        public double[] Resolve(Dictionary<string, object> reference, Params owner, string name)
        {
            string where = $"{owner.Tool}.{name}";
            var r = SnapRules.Parse(reference, where);
            if (_depth > 3) throw ToolException.Invalid($"{where}: snap references nest too deep.");
            _depth++;
            try
            {
                double[] from = r.From;
                if (r.FromParam != null)
                {
                    if (r.FromParam == name) throw ToolException.Invalid($"{where}: \"from\" cannot name the parameter itself.");
                    if (!owner.Has(r.FromParam)) throw ToolException.Invalid($"{where}: \"from\": this call has no parameter '{r.FromParam}'.");
                    from = owner.Point(r.FromParam);
                }
                var shapes = r.Of.Select(h => ShapeOf(h, where)).ToList();
                return SnapRules.Resolve(r, shapes, from, where);
            }
            finally { _depth--; }
        }

        private VShape ShapeOf(string reference, string where)
        {
            string handle = reference.StartsWith("@", StringComparison.Ordinal) ? FromBatch(reference, where) : reference;
            ObjectId id;
            try { id = Acad.IdFromHandle(_ctx.Db, handle); }
            catch (ToolException) { throw new ToolException(ErrorCodes.EntityNotFound, $"{where}: no object with handle {handle} (snap \"of\"). Use handles from tool results."); }
            if (_ctx.Tr != null) return ToShapeChecked(_ctx.Tr.GetObject(id, OpenMode.ForRead, false), handle, where);
            using (var tr = _ctx.Db.TransactionManager.StartOpenCloseTransaction())
            {
                var s = ToShapeChecked(tr.GetObject(id, OpenMode.ForRead, false), handle, where);
                tr.Commit();
                return s;
            }
        }

        private static VShape ToShapeChecked(DBObject o, string handle, string where)
        {
            if (!(o is Entity e)) throw new ToolException(ErrorCodes.EntityNotFound, $"{where}: {handle} is not a drawing object.");
            return DrawingService.ToShape(e);
        }

        private string FromBatch(string reference, string where)
        {
            var results = _ctx.BatchResults;
            if (results == null) throw ToolException.Invalid($"{where}: \"{reference}\" refers to a call of batch_execute; outside a batch use the handle itself.");
            var parts = reference.Substring(1).Split(':');
            int n = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int k = parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
            if (n >= results.Count) throw ToolException.Invalid($"{where}: \"{reference}\" refers to call {n}, which has not run yet (calls are numbered from 0; only earlier calls can be referenced).");
            var entry = results[n] as Dictionary<string, object>;
            if (entry == null || !(entry.TryGetValue("ok", out var ok) && ok is bool b && b))
                throw ToolException.Invalid($"{where}: \"{reference}\": call {n} failed, so it created nothing to snap to.");
            var res = entry.TryGetValue("result", out var rv) ? rv as Dictionary<string, object> : null;
            var hs = new List<string>();
            if (res != null)
            {
                if (res.TryGetValue("handle", out var h) && h is string hs1) hs.Add(hs1);
                if (res.TryGetValue("handles", out var hl) && hl is List<object> l) hs.AddRange(l.OfType<string>().Where(x => !hs.Contains(x)));
            }
            if (k >= hs.Count) throw ToolException.Invalid($"{where}: \"{reference}\": call {n} created {hs.Count} object(s).");
            return hs[k];
        }
    }

    public static partial class DrawingService
    {
        private static void RegisterSnap(ToolDispatcher d)
        {
            d.Register("get_snap_points", false, GetSnapPoints);
        }

        /// <summary>Characteristic points (end, mid, cen, ins) of the objects and the intersections between them.</summary>
        private static object GetSnapPoints(ToolContext ctx, Params p)
        {
            var hs = p.Handles("handles", 50);
            var shapes = new List<VShape>();
            foreach (var h in hs) shapes.Add(ToShape(Acad.GetEntity(ctx, h, false)));
            var pts = SnapRules.PointsOf(shapes);
            var objects = shapes.Select(s => (object)new Dictionary<string, object>
            {
                ["handle"] = s.Handle,
                ["type"] = s.Type == "OTHER" ? "OTHER" : s.Type,
                ["layer"] = s.Layer,
                ["points"] = pts.Where(x => x.Kind != "int" && x.Handles[0] == s.Handle)
                    .GroupBy(x => x.Kind)
                    .ToDictionary(g => g.Key, g => (object)g.Select(x => (object)new List<object> { Acad.R(x.P[0]), Acad.R(x.P[1]) }).ToList()),
            }).ToList();
            var r = new Dictionary<string, object> { ["objects"] = objects };
            var ints = pts.Where(x => x.Kind == "int").ToList();
            if (ints.Count > 0)
                r["intersections"] = ints.Select(x => (object)new Dictionary<string, object>
                {
                    ["of"] = x.Handles.Cast<object>().ToList(),
                    ["point"] = new List<object> { Acad.R(x.P[0]), Acad.R(x.P[1]) },
                }).ToList();
            return r;
        }
    }
}
