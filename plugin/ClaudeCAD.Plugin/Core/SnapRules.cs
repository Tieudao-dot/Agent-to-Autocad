using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>
    /// A point given by reference to drawing objects instead of numbers (docs/ke-hoach-tu-kiem.md §9, layer A):
    /// {"snap": "end", "of": "2F1A", "near": [3890, 2550], "offset": [0, -110]}. The plugin computes it like
    /// AutoCAD's object snaps, so Claude never adds and subtracts coordinates in its head.
    /// </summary>
    public sealed class SnapRef
    {
        public string Kind;
        /// <summary>Handles, or "@n" / "@n:k" (object created by call n of the same batch_execute).</summary>
        public List<string> Of = new List<string>();
        public double[] Near;
        public double[] Offset;
        /// <summary>per: the point the perpendicular starts from.</summary>
        public double[] From;
        /// <summary>per: name of another point parameter of the same call ("start").</summary>
        public string FromParam;
    }

    /// <summary>A characteristic point of an object (get_snap_points).</summary>
    public sealed class SnapPoint
    {
        public string Kind;
        public double[] P;
        public List<string> Handles = new List<string>();
    }

    /// <summary>
    /// Resolves snap references against objects of the drawing; also tells the plugin's point parser
    /// (Params.Point) where references are resolved. AutoCAD-independent: objects are VShape.
    /// </summary>
    public interface ISnapResolver
    {
        double[] Resolve(Dictionary<string, object> reference, Params owner, string name);
    }

    public static class SnapRules
    {
        public static readonly string[] Kinds = { "end", "mid", "cen", "int", "per", "nea", "ins" };
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static bool IsReference(object v) => v is Dictionary<string, object> d && d.ContainsKey("snap");

        public static bool IsObjectRef(string s) =>
            !string.IsNullOrEmpty(s) && (Params.IsHex(s) || System.Text.RegularExpressions.Regex.IsMatch(s, @"^@\d{1,3}(:\d{1,4})?$"));

        /// <summary>Validates a reference ({"snap", "of", "near"?, "offset"?, "from"?}). where = "draw_line.start".</summary>
        public static SnapRef Parse(Dictionary<string, object> d, string where)
        {
            string Bad(string msg) => $"{where}: {msg}";
            var r = new SnapRef();
            r.Kind = (d.TryGetValue("snap", out var k) ? k as string : null)?.Trim().ToLowerInvariant();
            if (!Kinds.Contains(r.Kind))
                throw ToolException.Invalid(Bad($"\"snap\" must be one of {string.Join(", ", Kinds)}."));
            if (!d.TryGetValue("of", out var of) || of == null)
                throw ToolException.Invalid(Bad("\"of\" is missing: the handle of the object to snap to (\"@n\" = object created by call n of this batch)."));
            if (of is string one) r.Of.Add(one.Trim());
            else if (of is List<object> many) foreach (var o in many) r.Of.Add((o as string ?? "").Trim());
            if (r.Of.Count == 0 || r.Of.Any(h => !IsObjectRef(h)))
                throw ToolException.Invalid(Bad("\"of\" must be a handle like \"2F1A\", \"@3\" (call 3 of this batch, 0-based) or a list of them."));
            int need = r.Kind == "int" ? 2 : 1;
            if (r.Of.Count != need)
                throw ToolException.Invalid(Bad(r.Kind == "int" ? "\"int\" needs \"of\": [handle1, handle2]." : $"\"{r.Kind}\" needs exactly one object in \"of\"."));
            r.Near = OptPoint(d, "near", Bad);
            r.Offset = OptPoint(d, "offset", Bad);
            if (d.TryGetValue("from", out var f) && f != null)
            {
                if (f is string fs) r.FromParam = fs.Trim();
                else if (Params.TryPoint(f, out var fp)) r.From = fp;
                else throw ToolException.Invalid(Bad("\"from\" must be a point [x, y] or the name of another point parameter of this call (e.g. \"start\")."));
            }
            if (r.Kind == "per" && r.From == null && r.FromParam == null)
                throw ToolException.Invalid(Bad("\"per\" needs \"from\": the point the perpendicular starts from."));
            if (r.Kind == "nea" && r.Near == null)
                throw ToolException.Invalid(Bad("\"nea\" needs \"near\": the point to project onto the object."));
            return r;
        }

        private static double[] OptPoint(Dictionary<string, object> d, string key, Func<string, string> bad)
        {
            if (!d.TryGetValue(key, out var v) || v == null) return null;
            if (Params.TryPoint(v, out var p)) return p;
            throw ToolException.Invalid(bad($"\"{key}\" must be a point [x, y]."));
        }

        /// <summary>Points of one kind on an object (end, mid, cen, ins).</summary>
        public static List<double[]> Candidates(string kind, VShape s)
        {
            var r = new List<double[]>();
            var segs = VerifyRules.Segments(s);
            switch (kind)
            {
                case "end":
                    if (s.Type == "LINE" || s.Type == "POLY") r.AddRange(s.Pts);
                    else if (s.Type == "ARC" && segs.Count > 0) { r.Add(segs[0].A); r.Add(segs[0].B); }
                    break;
                case "mid":
                    foreach (var g in segs)
                    {
                        if (s.Type == "CIRCLE") break;
                        r.Add(g.IsArc ? VerifyRules.PointOn(g.C, g.R, g.A0 + g.Sweep / 2) : new[] { (g.A[0] + g.B[0]) / 2, (g.A[1] + g.B[1]) / 2 });
                    }
                    break;
                case "cen":
                    if ((s.Type == "ARC" || s.Type == "CIRCLE") && s.Center != null) r.Add(s.Center);
                    else foreach (var g in segs.Where(g => g.IsArc)) r.Add(g.C);
                    break;
                case "ins":
                    if (s.Insert != null) r.Add(s.Insert);
                    break;
            }
            return Distinct(r);
        }

        private static List<double[]> Distinct(List<double[]> pts)
        {
            var r = new List<double[]>();
            foreach (var p in pts)
                if (!r.Any(q => VerifyRules.Dist(p, q) < 1e-6)) r.Add(p);
            return r;
        }

        /// <summary>Intersection points of two objects (their real segments, not their extensions).</summary>
        public static List<double[]> Intersections(VShape a, VShape b)
        {
            var r = new List<double[]>();
            foreach (var g in VerifyRules.Segments(a))
                foreach (var h in VerifyRules.Segments(b))
                    r.AddRange(Intersect(g, h));
            return Distinct(r);
        }

        private const double OnTol = 1e-6;

        private static IEnumerable<double[]> Intersect(VerifyRules.Seg g, VerifyRules.Seg h)
        {
            if (!g.IsArc && !h.IsArc)
            {
                var x = VerifyRules.LineLine(g.A, g.B, h.A, h.B);
                if (x != null && VerifyRules.DistToSeg(g, x) <= OnTol * Scale(g) && VerifyRules.DistToSeg(h, x) <= OnTol * Scale(h)) yield return x;
                yield break;
            }
            if (g.IsArc && h.IsArc)
            {
                foreach (var p in CircleCircle(g.C, g.R, h.C, h.R))
                    if (OnArc(g, p) && OnArc(h, p)) yield return p;
                yield break;
            }
            var line = g.IsArc ? h : g;
            var arc = g.IsArc ? g : h;
            foreach (var p in LineCircle(line.A, line.B, arc.C, arc.R))
                if (OnArc(arc, p)) yield return p;
        }

        private static double Scale(VerifyRules.Seg g) => Math.Max(1, VerifyRules.Dist(g.A, g.B));

        private static bool OnArc(VerifyRules.Seg s, double[] p) => VerifyRules.InSweep(s, Math.Atan2(p[1] - s.C[1], p[0] - s.C[0]));

        private static IEnumerable<double[]> LineCircle(double[] a, double[] b, double[] c, double r)
        {
            double dx = b[0] - a[0], dy = b[1] - a[1];
            double fx = a[0] - c[0], fy = a[1] - c[1];
            double A = dx * dx + dy * dy, B = 2 * (fx * dx + fy * dy), C = fx * fx + fy * fy - r * r;
            if (A < 1e-18) yield break;
            double disc = B * B - 4 * A * C;
            if (disc < -1e-9 * A * r * r) yield break;
            disc = Math.Max(0, disc);
            double sq = Math.Sqrt(disc);
            foreach (var t in disc < 1e-12 ? new[] { -B / (2 * A) } : new[] { (-B - sq) / (2 * A), (-B + sq) / (2 * A) })
                if (t >= -1e-9 && t <= 1 + 1e-9) yield return new[] { a[0] + t * dx, a[1] + t * dy };
        }

        private static IEnumerable<double[]> CircleCircle(double[] c1, double r1, double[] c2, double r2)
        {
            double d = VerifyRules.Dist(c1, c2);
            if (d < 1e-12 || d > r1 + r2 + 1e-9 || d < Math.Abs(r1 - r2) - 1e-9) yield break;
            double a = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
            double h = Math.Sqrt(Math.Max(0, r1 * r1 - a * a));
            double mx = c1[0] + a * (c2[0] - c1[0]) / d, my = c1[1] + a * (c2[1] - c1[1]) / d;
            double ox = -h * (c2[1] - c1[1]) / d, oy = h * (c2[0] - c1[0]) / d;
            yield return new[] { mx + ox, my + oy };
            if (h > 1e-9) yield return new[] { mx - ox, my - oy };
        }

        /// <summary>
        /// The point a reference names. shapes: the objects of r.Of, in order; from: the resolved "from" point (per).
        /// Throws ToolException with what to change when the reference is ambiguous or impossible.
        /// </summary>
        public static double[] Resolve(SnapRef r, IList<VShape> shapes, double[] from, string where)
        {
            var s = shapes[0];
            double[] p;
            switch (r.Kind)
            {
                case "int":
                    p = Pick(Intersections(shapes[0], shapes[1]), r, where,
                        $"{shapes[0].Handle} and {shapes[1].Handle} do not intersect (only real intersections count). Extend one of them first (extend_entities), or use \"per\" / \"end\".");
                    break;
                case "per":
                    p = Perpendicular(s, from, r.Near, where);
                    break;
                case "nea":
                    {
                        var segs = VerifyRules.Segments(s);
                        if (segs.Count == 0) throw ToolException.Invalid($"{where}: \"nea\" works on lines, polylines, arcs and circles; {s.Handle} is a {s.Type}.");
                        p = segs.Select(g => VerifyRules.Closest(g, r.Near)).OrderBy(q => VerifyRules.Dist(q, r.Near)).First();
                        break;
                    }
                default:
                    p = Pick(Candidates(r.Kind, s), r, where, $"{s.Handle} ({s.Type}) has no \"{r.Kind}\" point.");
                    break;
            }
            var res = new[] { p[0], p[1], 0.0 };
            if (r.Offset != null) { res[0] += r.Offset[0]; res[1] += r.Offset[1]; }
            return res;
        }

        private static double[] Pick(List<double[]> c, SnapRef r, string where, string none)
        {
            if (c.Count == 0) throw ToolException.Invalid($"{where}: {none}");
            if (c.Count == 1) return c[0];
            if (r.Near == null)
            {
                string list = string.Join(", ", c.Take(6).Select(Fmt)) + (c.Count > 6 ? ", ..." : "");
                throw ToolException.Invalid($"{where}: \"{r.Kind}\" of {string.Join("/", r.Of)} is ambiguous ({c.Count} points: {list}). Add \"near\": [x, y] close to the one you mean.");
            }
            return c.OrderBy(q => VerifyRules.Dist(q, r.Near)).First();
        }

        private static double[] Perpendicular(VShape s, double[] from, double[] near, string where)
        {
            var segs = VerifyRules.Segments(s);
            if (segs.Count == 0) throw ToolException.Invalid($"{where}: \"per\" works on lines, polylines, arcs and circles; {s.Handle} is a {s.Type}.");
            var probe = near ?? from;
            var g = segs.OrderBy(x => VerifyRules.DistToSeg(x, probe)).First();
            if (g.IsArc)
            {
                double dx = from[0] - g.C[0], dy = from[1] - g.C[1], l = Math.Sqrt(dx * dx + dy * dy);
                if (l < 1e-9) throw ToolException.Invalid($"{where}: \"from\" is the centre of {s.Handle}: every direction is perpendicular.");
                return new[] { g.C[0] + g.R * dx / l, g.C[1] + g.R * dy / l };
            }
            double ux = g.B[0] - g.A[0], uy = g.B[1] - g.A[1], len2 = ux * ux + uy * uy;
            double t = ((from[0] - g.A[0]) * ux + (from[1] - g.A[1]) * uy) / len2;
            return new[] { g.A[0] + t * ux, g.A[1] + t * uy }; // on the line or its extension, like AutoCAD
        }

        /// <summary>get_snap_points: characteristic points of the objects and their mutual intersections.</summary>
        public static List<SnapPoint> PointsOf(IList<VShape> shapes, int maxIntersections = 200)
        {
            var r = new List<SnapPoint>();
            foreach (var s in shapes)
                foreach (var kind in new[] { "end", "mid", "cen", "ins" })
                    foreach (var p in Candidates(kind, s))
                        r.Add(new SnapPoint { Kind = kind, P = p, Handles = { s.Handle } });
            int n = 0;
            for (int i = 0; i < shapes.Count && n < maxIntersections; i++)
                for (int j = i + 1; j < shapes.Count && n < maxIntersections; j++)
                    foreach (var p in Intersections(shapes[i], shapes[j]))
                    {
                        if (n++ >= maxIntersections) break;
                        r.Add(new SnapPoint { Kind = "int", P = p, Handles = { shapes[i].Handle, shapes[j].Handle } });
                    }
            return r;
        }

        private static string Fmt(double[] p) => $"[{p[0].ToString("0.###", Inv)}, {p[1].ToString("0.###", Inv)}]";
    }
}
