using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>
    /// One drawing object as the self-check sees it (AutoCAD-independent, 2D, WCS millimetres).
    /// Type: LINE, POLY (lightweight polyline), ARC, CIRCLE, TEXT (TEXT / MTEXT), DIM, OTHER.
    /// </summary>
    public sealed class VShape
    {
        public string Handle;
        public string Type;
        public string Layer;
        /// <summary>Created or changed by the turn: checked. false = a neighbour, used only as context.</summary>
        public bool OfTurn;
        /// <summary>Created by the turn (COUNT counts these).</summary>
        public bool IsNew;
        /// <summary>LINE: start, end. POLY: vertices. ARC: start point, end point.</summary>
        public List<double[]> Pts = new List<double[]>();
        /// <summary>POLY: bulge of the segment that starts at each vertex (null = all straight).</summary>
        public List<double> Bulges;
        public bool Closed;
        /// <summary>ARC / CIRCLE.</summary>
        public double[] Center;
        public double Radius;
        /// <summary>ARC: radians, counter-clockwise from +X (the arc runs CCW from start to end).</summary>
        public double StartAngle, EndAngle;
        /// <summary>TEXT / DIM: the 4 corners of the (possibly rotated) text box.</summary>
        public List<double[]> Box;
        /// <summary>Extents of the object (every type).</summary>
        public double[] Min, Max;
        /// <summary>TEXT: content (duplicate check).</summary>
        public string Text;
    }

    /// <summary>A safe automatic repair of one issue (verify_drawing auto_fix).</summary>
    public sealed class VFix
    {
        /// <summary>move_vertex | close | erase</summary>
        public string Kind;
        public string Handle;
        /// <summary>move_vertex: vertex / end index (LINE: 0 start, 1 end; POLY: vertex index).</summary>
        public int Vertex;
        public double[] To;
    }

    public sealed class VIssue
    {
        public string Code;
        public List<string> Handles = new List<string>();
        /// <summary>Short Vietnamese description (shown in the "Tự kiểm" card, read by Claude too).</summary>
        public string Message;
        /// <summary>How to fix it (for Claude, English).</summary>
        public string Hint;
        /// <summary>Where (for the card / zoom), may be null.</summary>
        public double[] At;
        /// <summary>Automatic repair, null when Claude has to fix it.</summary>
        public VFix Fix;
    }

    public sealed class VOptions
    {
        /// <summary>Largest gap / overshoot / open polyline treated as a mistake (mm). Above: intentional.</summary>
        public double Gap = VerifyRules.DefaultGap;
        /// <summary>Two points closer than this coincide (mm).</summary>
        public double Tol = VerifyRules.DefaultTol;
        /// <summary>Codes not reported (Claude / the skill judged them intentional).</summary>
        public HashSet<string> Ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Expected count of new objects per layer (COUNT), null = no COUNT check.</summary>
        public Dictionary<string, int> Expect;
        public int MaxIssues = 30;
    }

    public sealed class VReport
    {
        public int Checked;
        public List<VIssue> Issues = new List<VIssue>();
        /// <summary>Issues beyond MaxIssues, by code.</summary>
        public Dictionary<string, int> Omitted = new Dictionary<string, int>(StringComparer.Ordinal);
        public int Total => Issues.Count + Omitted.Values.Sum();
    }

    /// <summary>One remaining issue in the "Tự kiểm" card.</summary>
    public sealed class SelfCheckItem
    {
        public string Code;
        public string Message;
        public List<string> Handles = new List<string>();
    }

    /// <summary>
    /// The last verify_drawing of a turn, as shown in the "Tự kiểm" card under the reply and kept in the chat history.
    /// </summary>
    public sealed class SelfCheckSummary
    {
        public const int MaxItems = 30;
        public int Checked;
        public int Fixed;
        /// <summary>Issues beyond the listed ones.</summary>
        public int Omitted;
        /// <summary>The drawing was changed again after this check.</summary>
        public bool Stale;
        public List<SelfCheckItem> Items = new List<SelfCheckItem>();

        public int Remaining => Items.Count + Omitted;
        public bool Ok => Remaining == 0;

        public string Headline
        {
            get
            {
                string s = Ok ? $"Tự kiểm: {Checked} đối tượng · không lỗi ✓" : $"Tự kiểm: {Checked} đối tượng · {Remaining} lỗi ✗";
                if (Fixed > 0) s += $" · đã tự sửa {Fixed}";
                if (Stale) s += " · có sửa sau lần kiểm";
                return s;
            }
        }

        /// <summary>Handles of every remaining issue (the card's "Chọn đối tượng lỗi").</summary>
        public List<string> AllHandles() => Items.SelectMany(i => i.Handles).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        public static SelfCheckSummary From(VReport rep, int fixedCount) => new SelfCheckSummary
        {
            Checked = rep.Checked,
            Fixed = fixedCount,
            Omitted = rep.Omitted.Values.Sum(),
            Items = rep.Issues.Take(MaxItems).Select(i => new SelfCheckItem { Code = i.Code, Message = i.Message, Handles = i.Handles.ToList() }).ToList(),
        };

        public Dictionary<string, object> ToDict() => new Dictionary<string, object>
        {
            ["checked"] = Checked,
            ["fixed"] = Fixed,
            ["omitted"] = Omitted,
            ["stale"] = Stale,
            ["items"] = Items.Select(i => (object)new Dictionary<string, object>
            {
                ["code"] = i.Code, ["message"] = i.Message, ["handles"] = i.Handles.Cast<object>().ToList(),
            }).ToList(),
        };

        public static SelfCheckSummary FromDict(Dictionary<string, object> d)
        {
            if (d == null) return null;
            int N(Dictionary<string, object> o, string k) => o.TryGetValue(k, out var v) && v is double x ? (int)x : 0;
            var s = new SelfCheckSummary
            {
                Checked = N(d, "checked"), Fixed = N(d, "fixed"), Omitted = N(d, "omitted"),
                Stale = d.TryGetValue("stale", out var st) && st is bool b && b,
            };
            if (d.TryGetValue("items", out var iv) && iv is List<object> il)
                foreach (var o in il.OfType<Dictionary<string, object>>().Take(MaxItems))
                    s.Items.Add(new SelfCheckItem
                    {
                        Code = o.TryGetValue("code", out var c) ? c as string : "",
                        Message = o.TryGetValue("message", out var m) ? m as string : "",
                        Handles = o.TryGetValue("handles", out var h) && h is List<object> hl ? hl.OfType<string>().ToList() : new List<string>(),
                    });
            return s;
        }
    }

    /// <summary>
    /// Self-check of what a chat turn drew (docs/ke-hoach-tu-kiem.md): measured by the machine, not by looking at
    /// a picture. Only objects of the turn are checked; neighbours count as what they may touch.
    /// </summary>
    public static class VerifyRules
    {
        public const double DefaultGap = 10.0;
        public const double DefaultTol = 0.01;

        public static readonly string[] Codes =
        {
            "GAP", "OVERSHOOT", "OPEN_POLY", "ZERO_LEN", "DUPLICATE", "ARC_MISMATCH", "LAYER_0", "TEXT_OVERLAP", "STRAY", "COUNT",
        };

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ------------------------------------------------------------------ segments

        private sealed class Seg
        {
            public VShape Shape;
            public int Index;
            public double[] A, B;
            public bool IsArc;
            public double[] C;
            public double R, A0, Sweep; // arc from angle A0, signed sweep (CCW > 0)
        }

        private sealed class End
        {
            public VShape Shape;
            public int Vertex;   // index in Pts that moves (LINE 0/1, POLY 0/n-1, ARC 0/1)
            public int SegIndex; // the segment this end belongs to
            public double[] P;
            public double[] Inner; // straight end segment: its other point (null for arcs)
        }

        private static List<Seg> Segments(VShape s)
        {
            var r = new List<Seg>();
            switch (s.Type)
            {
                case "LINE":
                    if (s.Pts.Count >= 2) r.Add(new Seg { Shape = s, Index = 0, A = s.Pts[0], B = s.Pts[1] });
                    break;
                case "POLY":
                    {
                        int n = s.Pts.Count;
                        int segs = s.Closed ? n : n - 1;
                        for (int i = 0; i < segs; i++)
                        {
                            var a = s.Pts[i];
                            var b = s.Pts[(i + 1) % n];
                            double bulge = s.Bulges != null && i < s.Bulges.Count ? s.Bulges[i] : 0;
                            r.Add(Math.Abs(bulge) > 1e-9 ? BulgeArc(s, i, a, b, bulge) : new Seg { Shape = s, Index = i, A = a, B = b });
                        }
                        break;
                    }
                case "ARC":
                    if (s.Center != null)
                        r.Add(new Seg
                        {
                            Shape = s, Index = 0, IsArc = true, C = s.Center, R = s.Radius, A0 = s.StartAngle,
                            Sweep = Geometry.NormalizeRad(s.EndAngle - s.StartAngle),
                            A = s.Pts.Count > 0 ? s.Pts[0] : PointOn(s.Center, s.Radius, s.StartAngle),
                            B = s.Pts.Count > 1 ? s.Pts[1] : PointOn(s.Center, s.Radius, s.EndAngle),
                        });
                    break;
                case "CIRCLE":
                    if (s.Center != null)
                        r.Add(new Seg { Shape = s, Index = 0, IsArc = true, C = s.Center, R = s.Radius, A0 = 0, Sweep = 2 * Math.PI });
                    break;
            }
            return r;
        }

        private static Seg BulgeArc(VShape s, int i, double[] a, double[] b, double bulge)
        {
            double theta = 4 * Math.Atan(bulge);
            double dx = b[0] - a[0], dy = b[1] - a[1];
            double c = Math.Sqrt(dx * dx + dy * dy);
            if (c < 1e-12) return new Seg { Shape = s, Index = i, A = a, B = b };
            double k = (1 - bulge * bulge) / (4 * bulge);
            // left normal of a->b, scaled by the chord
            var center = new[] { (a[0] + b[0]) / 2 - dy * k, (a[1] + b[1]) / 2 + dx * k };
            double r = Dist(center, a);
            return new Seg { Shape = s, Index = i, A = a, B = b, IsArc = true, C = center, R = r, A0 = Math.Atan2(a[1] - center[1], a[0] - center[0]), Sweep = theta };
        }

        private static double[] PointOn(double[] c, double r, double ang) => new[] { c[0] + r * Math.Cos(ang), c[1] + r * Math.Sin(ang) };

        private static double Dist(double[] a, double[] b)
        {
            double dx = b[0] - a[0], dy = b[1] - a[1];
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool InSweep(Seg s, double ang)
        {
            if (Math.Abs(s.Sweep) >= 2 * Math.PI - 1e-12) return true;
            double t = s.Sweep >= 0 ? Geometry.NormalizeRad(ang - s.A0) : Geometry.NormalizeRad(s.A0 - ang);
            return t <= Math.Abs(s.Sweep) + 1e-12;
        }

        /// <summary>Closest point of a segment to p.</summary>
        private static double[] Closest(Seg s, double[] p)
        {
            if (s.IsArc)
            {
                double ang = Math.Atan2(p[1] - s.C[1], p[0] - s.C[0]);
                if (InSweep(s, ang)) return PointOn(s.C, s.R, ang);
                return Dist(p, s.A) <= Dist(p, s.B) ? s.A : s.B;
            }
            double dx = s.B[0] - s.A[0], dy = s.B[1] - s.A[1];
            double len2 = dx * dx + dy * dy;
            if (len2 < 1e-18) return s.A;
            double t = ((p[0] - s.A[0]) * dx + (p[1] - s.A[1]) * dy) / len2;
            t = Math.Max(0, Math.Min(1, t));
            return new[] { s.A[0] + t * dx, s.A[1] + t * dy };
        }

        private static double DistToSeg(Seg s, double[] p) => Dist(p, Closest(s, p));

        /// <summary>Intersection of the infinite lines p1-p2 and q1-q2 (null when parallel).</summary>
        private static double[] LineLine(double[] p1, double[] p2, double[] q1, double[] q2)
        {
            double d1x = p2[0] - p1[0], d1y = p2[1] - p1[1], d2x = q2[0] - q1[0], d2y = q2[1] - q1[1];
            double den = d1x * d2y - d1y * d2x;
            double l1 = Math.Sqrt(d1x * d1x + d1y * d1y), l2 = Math.Sqrt(d2x * d2x + d2y * d2y);
            if (l1 < 1e-12 || l2 < 1e-12 || Math.Abs(den) < 1e-9 * l1 * l2) return null;
            double t = ((q1[0] - p1[0]) * d2y - (q1[1] - p1[1]) * d2x) / den;
            return new[] { p1[0] + t * d1x, p1[1] + t * d1y };
        }

        private static double Length(VShape s)
        {
            switch (s.Type)
            {
                case "CIRCLE": return 2 * Math.PI * s.Radius;
                case "ARC": return s.Radius * Geometry.NormalizeRad(s.EndAngle - s.StartAngle);
                case "LINE":
                case "POLY":
                    double l = 0;
                    foreach (var g in Segments(s)) l += g.IsArc ? g.R * Math.Abs(g.Sweep) : Dist(g.A, g.B);
                    return l;
                default: return double.NaN;
            }
        }

        private static List<End> Ends(VShape s)
        {
            var r = new List<End>();
            if (s.Type == "LINE" && s.Pts.Count >= 2)
            {
                r.Add(new End { Shape = s, Vertex = 0, SegIndex = 0, P = s.Pts[0], Inner = s.Pts[1] });
                r.Add(new End { Shape = s, Vertex = 1, SegIndex = 0, P = s.Pts[1], Inner = s.Pts[0] });
            }
            else if (s.Type == "POLY" && !s.Closed && s.Pts.Count >= 2)
            {
                int n = s.Pts.Count;
                bool firstStraight = s.Bulges == null || Math.Abs(s.Bulges[0]) < 1e-9;
                bool lastStraight = s.Bulges == null || n - 2 >= s.Bulges.Count || Math.Abs(s.Bulges[n - 2]) < 1e-9;
                r.Add(new End { Shape = s, Vertex = 0, SegIndex = 0, P = s.Pts[0], Inner = firstStraight ? s.Pts[1] : null });
                r.Add(new End { Shape = s, Vertex = n - 1, SegIndex = n - 2, P = s.Pts[n - 1], Inner = lastStraight ? s.Pts[n - 2] : null });
            }
            else if (s.Type == "ARC" && s.Center != null)
            {
                var segs = Segments(s);
                r.Add(new End { Shape = s, Vertex = 0, SegIndex = 0, P = segs[0].A });
                r.Add(new End { Shape = s, Vertex = 1, SegIndex = 0, P = segs[0].B });
            }
            return r;
        }

        /// <summary>Points other ends may meet: line / arc ends and every polyline vertex.</summary>
        private static IEnumerable<double[]> SnapPoints(VShape s)
        {
            if (s.Type == "LINE" || s.Type == "POLY") return s.Pts;
            if (s.Type == "ARC" && s.Center != null)
            {
                var g = Segments(s)[0];
                return new[] { g.A, g.B };
            }
            return Enumerable.Empty<double[]>();
        }

        // ------------------------------------------------------------------ check

        public static VReport Check(IList<VShape> shapes, VOptions o = null)
        {
            o = o ?? new VOptions();
            var all = new List<VIssue>();
            var turn = shapes.Where(s => s.OfTurn).ToList();
            var zero = new HashSet<VShape>();

            // ZERO_LEN first: such objects are left out of every other check
            foreach (var s in turn)
            {
                double len = Length(s);
                bool isZero = !double.IsNaN(len) && len <= o.Tol || (s.Type == "CIRCLE" || s.Type == "ARC") && s.Radius <= o.Tol;
                if (!isZero) continue;
                zero.Add(s);
                all.Add(new VIssue
                {
                    Code = "ZERO_LEN", Handles = { s.Handle }, At = First(s),
                    Message = $"{TypeVi(s)} dài gần 0 (layer {s.Layer})",
                    Hint = "Erase it (auto_fix does it).",
                    Fix = new VFix { Kind = "erase", Handle = s.Handle },
                });
            }

            var live = shapes.Where(s => !zero.Contains(s)).ToList();
            var liveTurn = turn.Where(s => !zero.Contains(s)).ToList();
            var segs = live.SelectMany(Segments).ToList();

            CheckDuplicates(live, liveTurn, o, all);
            CheckOpenPolys(liveTurn, o, all);
            CheckEnds(live, liveTurn, segs, o, all);
            CheckLayer0(liveTurn, all);
            CheckTextOverlap(live, liveTurn, all);
            CheckStray(liveTurn, all);
            CheckCount(turn, o, all);

            var rep = new VReport { Checked = turn.Count };
            foreach (var i in all)
            {
                if (o.Ignore != null && o.Ignore.Contains(i.Code)) continue;
                if (rep.Issues.Count < o.MaxIssues) rep.Issues.Add(i);
                else rep.Omitted[i.Code] = (rep.Omitted.TryGetValue(i.Code, out var n) ? n : 0) + 1;
            }
            return rep;
        }

        private static double[] First(VShape s) =>
            s.Pts.Count > 0 ? s.Pts[0] : s.Center ?? (s.Min != null && s.Max != null ? new[] { (s.Min[0] + s.Max[0]) / 2, (s.Min[1] + s.Max[1]) / 2 } : null);

        private static string TypeVi(VShape s)
        {
            switch (s.Type)
            {
                case "LINE": return "Đường";
                case "POLY": return "Polyline";
                case "ARC": return "Cung";
                case "CIRCLE": return "Đường tròn";
                case "TEXT": return "Chữ";
                case "DIM": return "Dim";
                default: return "Đối tượng";
            }
        }

        private static string Mm(double v) => v.ToString(v < 1 ? "0.###" : "0.#", Inv);

        private static string Pt(double[] p) => $"[{p[0].ToString("0.###", Inv)}, {p[1].ToString("0.###", Inv)}]";

        // ---- DUPLICATE

        private static void CheckDuplicates(List<VShape> live, List<VShape> liveTurn, VOptions o, List<VIssue> all)
        {
            var erased = new HashSet<VShape>();
            var index = new Dictionary<VShape, int>();
            for (int i = 0; i < live.Count; i++) index[live[i]] = i;
            foreach (var a in liveTurn)
            {
                if (erased.Contains(a)) continue;
                foreach (var b in live)
                {
                    if (ReferenceEquals(a, b) || erased.Contains(b)) continue;
                    if (b.OfTurn && index[b] < index[a]) continue; // the pair is handled from b
                    if (a.Type != b.Type || !string.Equals(a.Layer, b.Layer, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!SameGeometry(a, b, o.Tol)) continue;
                    // erase the copy of the turn: the later one when both are new
                    var victim = b.OfTurn ? b : a;
                    var keep = ReferenceEquals(victim, a) ? b : a;
                    erased.Add(victim);
                    all.Add(new VIssue
                    {
                        Code = "DUPLICATE", Handles = { victim.Handle, keep.Handle }, At = First(victim),
                        Message = $"{TypeVi(victim)} trùng hoàn toàn với đối tượng khác (layer {victim.Layer})",
                        Hint = $"Erase {victim.Handle} (auto_fix does it).",
                        Fix = new VFix { Kind = "erase", Handle = victim.Handle },
                    });
                    if (ReferenceEquals(victim, a)) break;
                }
            }
        }

        private static bool Near(double[] p, double[] q, double tol) => p != null && q != null && Dist(p, q) <= tol;

        private static bool SameGeometry(VShape a, VShape b, double tol)
        {
            switch (a.Type)
            {
                case "LINE":
                    return a.Pts.Count >= 2 && b.Pts.Count >= 2 &&
                           (Near(a.Pts[0], b.Pts[0], tol) && Near(a.Pts[1], b.Pts[1], tol) || Near(a.Pts[0], b.Pts[1], tol) && Near(a.Pts[1], b.Pts[0], tol));
                case "CIRCLE":
                    return Near(a.Center, b.Center, tol) && Math.Abs(a.Radius - b.Radius) <= tol;
                case "ARC":
                    {
                        if (!Near(a.Center, b.Center, tol) || Math.Abs(a.Radius - b.Radius) > tol) return false;
                        var ga = Segments(a)[0];
                        var gb = Segments(b)[0];
                        return Near(ga.A, gb.A, tol) && Near(ga.B, gb.B, tol);
                    }
                case "POLY":
                    return SamePoly(a, b, tol);
                case "TEXT":
                    return a.Text != null && a.Text == b.Text && a.Box != null && b.Box != null && Near(a.Box[0], b.Box[0], tol) && Near(a.Box[2], b.Box[2], tol);
                default:
                    return false;
            }
        }

        private static double BulgeAt(VShape s, int i) => s.Bulges != null && i >= 0 && i < s.Bulges.Count ? s.Bulges[i] : 0;

        private static bool SamePoly(VShape a, VShape b, double tol)
        {
            int n = a.Pts.Count;
            if (n != b.Pts.Count || a.Closed != b.Closed || n < 2) return false;
            int segs = a.Closed ? n : n - 1;
            int shifts = a.Closed ? n : 1;
            for (int k = 0; k < shifts; k++)
            {
                int kk = k;
                Func<int, int> fwd = i => (i + kk) % n;
                Func<int, int> rev = a.Closed ? (Func<int, int>)(i => ((kk - i) % n + n) % n) : i => n - 1 - i;
                bool f = true, r = true;
                for (int i = 0; i < n; i++)
                {
                    f = f && Near(a.Pts[i], b.Pts[fwd(i)], tol);
                    r = r && Near(a.Pts[i], b.Pts[rev(i)], tol);
                }
                for (int i = 0; i < segs; i++)
                {
                    // forward: same segment index; reversed: b's segment from rev(i + 1) to rev(i), opposite bulge
                    f = f && Math.Abs(BulgeAt(a, i) - BulgeAt(b, fwd(i))) < 1e-6;
                    r = r && Math.Abs(BulgeAt(a, i) + BulgeAt(b, rev((i + 1) % n))) < 1e-6;
                }
                if (f || r) return true;
            }
            return false;
        }

        // ---- OPEN_POLY

        private static void CheckOpenPolys(List<VShape> liveTurn, VOptions o, List<VIssue> all)
        {
            foreach (var s in liveTurn)
            {
                if (s.Type != "POLY" || s.Closed || s.Pts.Count < 3) continue;
                double d = Dist(s.Pts[0], s.Pts[s.Pts.Count - 1]);
                if (d > o.Gap) continue;
                bool canFix = s.Pts.Count >= 4;
                all.Add(new VIssue
                {
                    Code = "OPEN_POLY", Handles = { s.Handle }, At = s.Pts[0],
                    Message = d <= o.Tol
                        ? $"Polyline khép kín nhưng chưa đóng (layer {s.Layer})"
                        : $"Polyline hở {Mm(d)} mm giữa điểm đầu và điểm cuối (layer {s.Layer})",
                    Hint = canFix ? "Close it (auto_fix removes the last vertex and sets closed)." : "Redraw it as a closed polyline.",
                    Fix = canFix ? new VFix { Kind = "close", Handle = s.Handle } : null,
                });
            }
        }

        // ---- GAP / OVERSHOOT / ARC_MISMATCH

        private static void CheckEnds(List<VShape> live, List<VShape> liveTurn, List<Seg> segs, VOptions o, List<VIssue> all)
        {
            var reportedPairs = new HashSet<string>(StringComparer.Ordinal);
            var movedEnds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in liveTurn)
            {
                foreach (var e in Ends(s))
                {
                    if (Touches(e, segs, o.Tol)) continue;

                    // OVERSHOOT: the straight end segment crosses another object just before its end
                    if (e.Inner != null)
                    {
                        var x = Overshoot(e, segs, o);
                        if (x != null)
                        {
                            double d = Dist(e.P, x.Item1);
                            all.Add(new VIssue
                            {
                                Code = "OVERSHOOT", Handles = { s.Handle, x.Item2.Handle }, At = e.P,
                                Message = $"Nét thừa {Mm(d)} mm qua giao điểm (layer {s.Layer})",
                                Hint = $"Trim the end of {s.Handle} back to {Pt(x.Item1)} (trim_entities, or edit the end point).",
                            });
                            continue;
                        }
                    }

                    var g = NearestGap(e, live, segs, o);
                    if (g != null)
                    {
                        string key = PairKey(e, g);
                        if (!reportedPairs.Add(key)) continue;
                        VFix fix = null;
                        string mk = s.Handle + ":" + e.Vertex.ToString(Inv);
                        if ((s.Type == "LINE" || s.Type == "POLY") && !movedEnds.Contains(mk))
                        {
                            fix = new VFix { Kind = "move_vertex", Handle = s.Handle, Vertex = e.Vertex, To = g.Target };
                            movedEnds.Add(mk);
                        }
                        else if (g.OtherEnd != null && g.OtherEnd.Shape.OfTurn && (g.OtherEnd.Shape.Type == "LINE" || g.OtherEnd.Shape.Type == "POLY"))
                        {
                            fix = new VFix { Kind = "move_vertex", Handle = g.OtherEnd.Shape.Handle, Vertex = g.OtherEnd.Vertex, To = e.P };
                            movedEnds.Add(g.OtherEnd.Shape.Handle + ":" + g.OtherEnd.Vertex.ToString(Inv));
                        }
                        all.Add(new VIssue
                        {
                            Code = "GAP", Handles = { s.Handle, g.Other.Handle }, At = e.P,
                            Message = $"Khe hở {Mm(g.Distance)} mm ở đầu nét (layer {s.Layer})",
                            Hint = fix != null
                                ? $"Move the end {Pt(e.P)} of {s.Handle} to {Pt(g.Target)} (auto_fix does it)."
                                : $"Make the end {Pt(e.P)} of {s.Handle} meet {g.Other.Handle} at {Pt(g.Target)}.",
                            Fix = fix,
                        });
                        continue;
                    }

                    if (s.Type == "ARC")
                    {
                        all.Add(new VIssue
                        {
                            Code = "ARC_MISMATCH", Handles = { s.Handle }, At = e.P,
                            Message = $"Đầu cung không chạm đối tượng nào (layer {s.Layer})",
                            Hint = $"The arc end {Pt(e.P)} of {s.Handle} touches nothing: a door swing must end on the leaf / the jamb. Redraw the arc from the right points.",
                        });
                    }
                }
            }
        }

        private static string PairKey(End e, GapHit g)
        {
            string a = e.Shape.Handle + ":" + e.Vertex.ToString(Inv);
            string b = g.OtherEnd != null ? g.OtherEnd.Shape.Handle + ":" + g.OtherEnd.Vertex.ToString(Inv) : g.Other.Handle + "@" + Pt(g.Target);
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }

        private static bool Touches(End e, List<Seg> segs, double tol)
        {
            foreach (var g in segs)
            {
                if (ReferenceEquals(g.Shape, e.Shape))
                {
                    if (e.Shape.Type != "POLY" || g.Index == e.SegIndex) continue;
                }
                if (DistToSeg(g, e.P) <= tol) return true;
            }
            return false;
        }

        private static Tuple<double[], VShape> Overshoot(End e, List<Seg> segs, VOptions o)
        {
            double best = double.MaxValue;
            Tuple<double[], VShape> hit = null;
            double segLen = Dist(e.Inner, e.P);
            foreach (var g in segs)
            {
                if (g.IsArc || ReferenceEquals(g.Shape, e.Shape)) continue;
                var x = LineLine(e.Inner, e.P, g.A, g.B);
                if (x == null) continue;
                // x on the other segment, and on our end segment within the overshoot distance from the end
                if (DistToSeg(g, x) > o.Tol) continue;
                double fromEnd = Dist(x, e.P), fromInner = Dist(x, e.Inner);
                if (fromEnd <= o.Tol || fromEnd > o.Gap || fromInner > segLen + o.Tol || fromInner <= o.Tol) continue;
                if (Math.Abs(fromEnd + fromInner - segLen) > o.Tol * 10) continue; // must lie between the two points
                if (fromEnd < best) { best = fromEnd; hit = Tuple.Create(x, g.Shape); }
            }
            return hit;
        }

        private sealed class GapHit
        {
            public VShape Other;
            public End OtherEnd; // the other object's free end, when the gap is end-to-end
            public double[] Target;
            public double Distance;
        }

        private static GapHit NearestGap(End e, List<VShape> live, List<Seg> segs, VOptions o)
        {
            GapHit best = null;
            // end-to-point first (corners): a free end of a line / arc, or a polyline vertex
            foreach (var s in live)
            {
                if (ReferenceEquals(s, e.Shape)) continue;
                foreach (var p in SnapPoints(s))
                {
                    double d = Dist(e.P, p);
                    if (d <= o.Tol || d > o.Gap) continue;
                    if (best == null || d < best.Distance)
                        best = new GapHit { Other = s, Target = p, Distance = d, OtherEnd = Ends(s).FirstOrDefault(x => Near(x.P, p, 1e-9)) };
                }
            }
            // end-to-body (T junction not reached)
            foreach (var g in segs)
            {
                if (ReferenceEquals(g.Shape, e.Shape)) continue;
                var c = Closest(g, e.P);
                double d = Dist(e.P, c);
                if (d <= o.Tol || d > o.Gap) continue;
                if (best != null && best.Distance <= d + o.Tol) continue; // a corner as near wins
                var target = c;
                if (e.Inner != null && !g.IsArc)
                {
                    // extend along the line itself, so it stays straight
                    var x = LineLine(e.Inner, e.P, g.A, g.B);
                    if (x != null && DistToSeg(g, x) <= o.Tol && Dist(x, e.P) <= o.Gap) target = x;
                }
                best = new GapHit { Other = g.Shape, Target = target, Distance = d };
            }
            return best;
        }

        // ---- LAYER_0

        private static void CheckLayer0(List<VShape> liveTurn, List<VIssue> all)
        {
            var bad = liveTurn.Where(s => string.Equals(s.Layer, "0", StringComparison.Ordinal) || string.Equals(s.Layer, "DEFPOINTS", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var grp in bad.GroupBy(s => s.Layer.ToUpperInvariant()))
            {
                var list = grp.ToList();
                all.Add(new VIssue
                {
                    Code = "LAYER_0", Handles = list.Select(s => s.Handle).ToList(), At = First(list[0]),
                    Message = $"{list.Count} đối tượng trên layer {list[0].Layer}",
                    Hint = "Move them to the right layer of the standard (set_entity_props layer).",
                });
            }
        }

        // ---- TEXT_OVERLAP

        private static void CheckTextOverlap(List<VShape> live, List<VShape> liveTurn, List<VIssue> all)
        {
            var boxes = live.Where(s => (s.Type == "TEXT" || s.Type == "DIM") && s.Box != null && s.Box.Count == 4).ToList();
            var done = new HashSet<string>(StringComparer.Ordinal);
            foreach (var a in liveTurn)
            {
                if ((a.Type != "TEXT" && a.Type != "DIM") || a.Box == null || a.Box.Count != 4) continue;
                var sa = Shrink(a.Box, 0.9);
                foreach (var b in boxes)
                {
                    if (ReferenceEquals(a, b)) continue;
                    string key = string.CompareOrdinal(a.Handle, b.Handle) <= 0 ? a.Handle + "|" + b.Handle : b.Handle + "|" + a.Handle;
                    if (done.Contains(key)) continue;
                    if (!ConvexOverlap(sa, Shrink(b.Box, 0.9))) continue;
                    done.Add(key);
                    all.Add(new VIssue
                    {
                        Code = "TEXT_OVERLAP", Handles = { a.Handle, b.Handle }, At = Centroid(a.Box),
                        Message = $"{TypeVi(a)} chồng lên {TypeVi(b).ToLowerInvariant()} khác (layer {a.Layer})",
                        Hint = $"Move {a.Handle} (or {b.Handle}) so the two do not overlap.",
                    });
                }
            }
        }

        private static double[] Centroid(List<double[]> box) =>
            new[] { box.Average(p => p[0]), box.Average(p => p[1]) };

        private static List<double[]> Shrink(List<double[]> box, double k)
        {
            var c = Centroid(box);
            return box.Select(p => new[] { c[0] + (p[0] - c[0]) * k, c[1] + (p[1] - c[1]) * k }).ToList();
        }

        /// <summary>Separating axis test for two convex polygons.</summary>
        private static bool ConvexOverlap(List<double[]> a, List<double[]> b)
        {
            foreach (var poly in new[] { a, b })
            {
                for (int i = 0; i < poly.Count; i++)
                {
                    var p = poly[i];
                    var q = poly[(i + 1) % poly.Count];
                    double nx = -(q[1] - p[1]), ny = q[0] - p[0];
                    if (Math.Abs(nx) < 1e-12 && Math.Abs(ny) < 1e-12) continue;
                    double amin = double.MaxValue, amax = double.MinValue, bmin = double.MaxValue, bmax = double.MinValue;
                    foreach (var v in a) { double t = v[0] * nx + v[1] * ny; amin = Math.Min(amin, t); amax = Math.Max(amax, t); }
                    foreach (var v in b) { double t = v[0] * nx + v[1] * ny; bmin = Math.Min(bmin, t); bmax = Math.Max(bmax, t); }
                    if (amax <= bmin || bmax <= amin) return false;
                }
            }
            return true;
        }

        // ---- STRAY

        private static void CheckStray(List<VShape> liveTurn, List<VIssue> all)
        {
            var withBox = liveTurn.Where(s => s.Min != null && s.Max != null).ToList();
            if (withBox.Count < 3) return;
            var cx = withBox.Select(s => (s.Min[0] + s.Max[0]) / 2).ToList();
            var cy = withBox.Select(s => (s.Min[1] + s.Max[1]) / 2).ToList();
            double mx = Median(cx), my = Median(cy);
            var d = withBox.Select((s, i) => Math.Sqrt((cx[i] - mx) * (cx[i] - mx) + (cy[i] - my) * (cy[i] - my))).ToList();
            var size = withBox.Select(s => Math.Sqrt((s.Max[0] - s.Min[0]) * (s.Max[0] - s.Min[0]) + (s.Max[1] - s.Min[1]) * (s.Max[1] - s.Min[1]))).ToList();
            double k = Math.Max(Percentile(d, 0.75), Math.Max(Median(size), 1.0));
            for (int i = 0; i < withBox.Count; i++)
            {
                if (d[i] - size[i] / 2 <= 3 * k) continue;
                var s = withBox[i];
                all.Add(new VIssue
                {
                    Code = "STRAY", Handles = { s.Handle }, At = new[] { cx[i], cy[i] },
                    Message = $"{TypeVi(s)} nằm xa cụm vừa vẽ ({Mm(d[i] / 1000)} m, layer {s.Layer})",
                    Hint = $"{s.Handle} is far from the rest of what this turn drew: check its position (wrong coordinate?).",
                });
            }
        }

        private static double Median(List<double> v) => Percentile(v, 0.5);

        private static double Percentile(List<double> v, double q)
        {
            if (v.Count == 0) return 0;
            var s = v.OrderBy(x => x).ToList();
            double pos = q * (s.Count - 1);
            int lo = (int)Math.Floor(pos), hi = (int)Math.Ceiling(pos);
            return s[lo] + (s[hi] - s[lo]) * (pos - lo);
        }

        // ---- COUNT

        private static void CheckCount(List<VShape> turn, VOptions o, List<VIssue> all)
        {
            if (o.Expect == null || o.Expect.Count == 0) return;
            var fresh = turn.Where(s => s.IsNew).ToList();
            var byLayer = fresh.GroupBy(s => s.Layer ?? "", StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (var kv in o.Expect)
            {
                int actual = byLayer.TryGetValue(kv.Key, out var l) ? l.Count : 0;
                if (actual == kv.Value) continue;
                all.Add(new VIssue
                {
                    Code = "COUNT", Handles = l?.Select(s => s.Handle).ToList() ?? new List<string>(),
                    Message = $"Layer {kv.Key}: dự kiến {kv.Value} đối tượng, vẽ được {actual}",
                    Hint = actual < kv.Value ? "Something planned is missing: draw it, or explain why not." : "More objects than planned: look for extra / duplicate ones.",
                });
            }
            foreach (var kv in byLayer)
            {
                if (o.Expect.Keys.Any(k => string.Equals(k, kv.Key, StringComparison.OrdinalIgnoreCase))) continue;
                all.Add(new VIssue
                {
                    Code = "COUNT", Handles = kv.Value.Select(s => s.Handle).ToList(),
                    Message = $"Layer {kv.Key}: {kv.Value.Count} đối tượng ngoài danh mục dự kiến",
                    Hint = "Objects on a layer the plan did not list: check they belong there.",
                });
            }
        }

        // ------------------------------------------------------------------ fixes

        /// <summary>The automatic repairs of a report, without conflicts: erases first, then one move per end.</summary>
        public static List<VFix> SelectFixes(VReport rep)
        {
            var fixes = rep.Issues.Where(i => i.Fix != null).Select(i => i.Fix).ToList();
            var erased = new HashSet<string>(fixes.Where(f => f.Kind == "erase").Select(f => f.Handle), StringComparer.OrdinalIgnoreCase);
            var r = fixes.Where(f => f.Kind == "erase").GroupBy(f => f.Handle, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
            var closed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in fixes.Where(f => f.Kind == "close"))
                if (!erased.Contains(f.Handle) && closed.Add(f.Handle)) r.Add(f);
            var moved = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in fixes.Where(f => f.Kind == "move_vertex"))
            {
                if (erased.Contains(f.Handle)) continue;
                if (closed.Contains(f.Handle)) continue; // closing renumbers the vertices
                if (moved.Add(f.Handle.ToUpperInvariant() + ":" + f.Vertex.ToString(Inv))) r.Add(f);
            }
            return r;
        }

        /// <summary>Applies fixes to shapes (copies): used by tests, and documents what the plugin does on the entities.</summary>
        public static List<VShape> Simulate(IList<VShape> shapes, IEnumerable<VFix> fixes)
        {
            var copy = shapes.Select(Clone).ToList();
            foreach (var f in fixes)
            {
                var s = copy.FirstOrDefault(x => string.Equals(x.Handle, f.Handle, StringComparison.OrdinalIgnoreCase));
                if (s == null) continue;
                switch (f.Kind)
                {
                    case "erase":
                        copy.Remove(s);
                        break;
                    case "close":
                        s.Pts.RemoveAt(s.Pts.Count - 1);
                        if (s.Bulges != null && s.Bulges.Count > s.Pts.Count) s.Bulges.RemoveAt(s.Bulges.Count - 1);
                        s.Closed = true;
                        break;
                    case "move_vertex":
                        s.Pts[f.Vertex] = new[] { f.To[0], f.To[1] };
                        break;
                }
            }
            return copy;
        }

        private static VShape Clone(VShape s) => new VShape
        {
            Handle = s.Handle, Type = s.Type, Layer = s.Layer, OfTurn = s.OfTurn, IsNew = s.IsNew,
            Pts = s.Pts.Select(p => (double[])p.Clone()).ToList(), Bulges = s.Bulges?.ToList(), Closed = s.Closed,
            Center = s.Center, Radius = s.Radius, StartAngle = s.StartAngle, EndAngle = s.EndAngle,
            Box = s.Box, Min = s.Min, Max = s.Max, Text = s.Text,
        };

        /// <summary>Short Vietnamese label of a code (the "Tự kiểm" card).</summary>
        public static string Label(string code)
        {
            switch (code)
            {
                case "GAP": return "khe hở";
                case "OVERSHOOT": return "nét thừa";
                case "OPEN_POLY": return "polyline chưa đóng";
                case "ZERO_LEN": return "đối tượng dài 0";
                case "DUPLICATE": return "đối tượng trùng";
                case "ARC_MISMATCH": return "cung không chạm";
                case "LAYER_0": return "sai layer";
                case "TEXT_OVERLAP": return "chữ đè nhau";
                case "STRAY": return "đối tượng lạc";
                case "COUNT": return "thiếu / thừa";
                default: return code;
            }
        }
    }
}
