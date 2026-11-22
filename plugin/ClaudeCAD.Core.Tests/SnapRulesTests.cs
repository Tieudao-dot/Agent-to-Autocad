using System;
using System.Collections.Generic;
using System.Linq;
using ClaudeCAD.Plugin.Core;
using Xunit;

namespace ClaudeCAD.Core.Tests
{
    public class SnapRulesTests
    {
        private static double[] P(double x, double y) => new[] { x, y };
        private static List<object> J(double x, double y) => new List<object> { x, y };

        private static VShape Line(string h, double x1, double y1, double x2, double y2) =>
            new VShape { Handle = h, Type = "LINE", Layer = "A-WALL", Pts = { P(x1, y1), P(x2, y2) } };

        private static VShape Arc(string h, double cx, double cy, double r, double s, double e) =>
            new VShape { Handle = h, Type = "ARC", Layer = "A-DOOR", Center = P(cx, cy), Radius = r, StartAngle = s * Math.PI / 180, EndAngle = e * Math.PI / 180 };

        private static VShape Rect(string h) => new VShape
        {
            Handle = h, Type = "POLY", Layer = "A-WALL", Closed = true,
            Pts = { P(-110, -110), P(4110, -110), P(4110, 6110), P(-110, 6110) },
        };

        private static Dictionary<string, object> Ref(string kind, object of, List<object> near = null, List<object> offset = null, object from = null)
        {
            var d = new Dictionary<string, object> { ["snap"] = kind, ["of"] = of };
            if (near != null) d["near"] = near;
            if (offset != null) d["offset"] = offset;
            if (from != null) d["from"] = from;
            return d;
        }

        private static double[] Resolve(Dictionary<string, object> d, params VShape[] shapes) =>
            SnapRules.Resolve(SnapRules.Parse(d, "t.p"), shapes, null, "t.p");

        private static void Near(double[] expected, double[] actual) =>
            Assert.True(Math.Abs(expected[0] - actual[0]) < 1e-6 && Math.Abs(expected[1] - actual[1]) < 1e-6,
                $"expected [{expected[0]}, {expected[1]}], got [{actual[0]}, {actual[1]}]");

        [Fact]
        public void EndPicksTheEndNearTheHint()
        {
            var l = Line("2A", 0, 0, 4000, 0);
            Near(P(4000, 0), Resolve(Ref("end", "2A", J(3900, 10)), l));
            Near(P(0, 0), Resolve(Ref("end", "2A", J(-5, 0)), l));
        }

        [Fact]
        public void AmbiguousEndWithoutHintIsAnErrorListingTheCandidates()
        {
            var ex = Assert.Throws<ToolException>(() => Resolve(Ref("end", "2A"), Line("2A", 0, 0, 4000, 0)));
            Assert.Contains("ambiguous", ex.Message);
            Assert.Contains("[4000, 0]", ex.Message);
            Assert.Contains("near", ex.Message);
        }

        [Fact]
        public void EndOfAPolylineIsAVertexAndOffsetIsAdded()
        {
            Near(P(4110, -110 + 2660), Resolve(Ref("end", "2B", J(4000, 0), J(0, 2660)), Rect("2B")));
        }

        [Fact]
        public void MidOfLineArcAndPolylineSegment()
        {
            Near(P(2000, 0), Resolve(Ref("mid", "2A"), Line("2A", 0, 0, 4000, 0)));
            Near(P(1000 + 900 * Math.Cos(Math.PI / 4), 900 * Math.Sin(Math.PI / 4)), Resolve(Ref("mid", "2C"), Arc("2C", 1000, 0, 900, 0, 90)));
            Near(P(4110, 3000), Resolve(Ref("mid", "2B", J(4200, 2900)), Rect("2B")));
        }

        [Fact]
        public void CenterOfArcAndCircle()
        {
            Near(P(1000, 0), Resolve(Ref("cen", "2C"), Arc("2C", 1000, 0, 900, 0, 90)));
            var c = new VShape { Handle = "2D", Type = "CIRCLE", Center = P(5, 6), Radius = 400 };
            Near(P(5, 6), Resolve(Ref("cen", "2D"), c));
            Assert.Throws<ToolException>(() => Resolve(Ref("cen", "2A"), Line("2A", 0, 0, 1, 0)));
        }

        [Fact]
        public void IntersectionOfTwoLinesAndOfALineAndAnArc()
        {
            Near(P(2000, 0), Resolve(Ref("int", new List<object> { "2A", "2E" }), Line("2A", 0, 0, 4000, 0), Line("2E", 2000, -500, 2000, 3000)));
            var arc = Arc("2C", 0, 0, 1000, 0, 180);
            Near(P(0, 1000), Resolve(Ref("int", new List<object> { "2C", "2E" }), arc, Line("2E", 0, -2000, 0, 2000)));
        }

        [Fact]
        public void NoRealIntersectionSaysToExtend()
        {
            var ex = Assert.Throws<ToolException>(() =>
                Resolve(Ref("int", new List<object> { "2A", "2E" }), Line("2A", 0, 0, 1000, 0), Line("2E", 2000, -500, 2000, 3000)));
            Assert.Contains("extend_entities", ex.Message);
        }

        [Fact]
        public void PerpendicularFootIsOnTheLineOrItsExtension()
        {
            var r = SnapRules.Parse(Ref("per", "2A", from: J(1500, 800)), "t.p");
            Near(P(1500, 0), SnapRules.Resolve(r, new[] { Line("2A", 0, 0, 4000, 0) }, r.From, "t.p"));
            var r2 = SnapRules.Parse(Ref("per", "2A", from: J(5000, 800)), "t.p");
            Near(P(5000, 0), SnapRules.Resolve(r2, new[] { Line("2A", 0, 0, 4000, 0) }, r2.From, "t.p"));
        }

        [Fact]
        public void NearestPointOnTheObject()
        {
            Near(P(1000, 0), Resolve(Ref("nea", "2A", J(1000, 37)), Line("2A", 0, 0, 4000, 0)));
            Assert.Throws<ToolException>(() => SnapRules.Parse(Ref("nea", "2A"), "t.p")); // near is required
        }

        [Fact]
        public void InsertionPointOfTextsAndBlocks()
        {
            var t = new VShape { Handle = "2F", Type = "TEXT", Insert = P(2000, 3100) };
            Near(P(2000, 3100), Resolve(Ref("ins", "2F"), t));
        }

        [Fact]
        public void InvalidReferencesAreRejectedWithClearMessages()
        {
            Assert.Contains("\"snap\" must be one of", Assert.Throws<ToolException>(() => SnapRules.Parse(Ref("tan", "2A"), "t.p")).Message);
            Assert.Contains("\"of\"", Assert.Throws<ToolException>(() => SnapRules.Parse(new Dictionary<string, object> { ["snap"] = "end" }, "t.p")).Message);
            Assert.Contains("\"of\"", Assert.Throws<ToolException>(() => SnapRules.Parse(Ref("end", "not a handle"), "t.p")).Message);
            Assert.Contains("[handle1, handle2]", Assert.Throws<ToolException>(() => SnapRules.Parse(Ref("int", "2A"), "t.p")).Message);
            Assert.Contains("\"from\"", Assert.Throws<ToolException>(() => SnapRules.Parse(Ref("per", "2A"), "t.p")).Message);
            Assert.True(SnapRules.IsObjectRef("@3"));
            Assert.True(SnapRules.IsObjectRef("@3:1"));
            Assert.False(SnapRules.IsObjectRef("@x"));
        }

        [Fact]
        public void SnapPointsListEveryKindAndTheIntersections()
        {
            var pts = SnapRules.PointsOf(new[] { Line("2A", 0, 0, 4000, 0), Line("2E", 2000, -500, 2000, 3000) });
            Assert.Equal(4, pts.Count(x => x.Kind == "end"));
            Assert.Equal(2, pts.Count(x => x.Kind == "mid"));
            var i = Assert.Single(pts, x => x.Kind == "int");
            Near(P(2000, 0), i.P);
            Assert.Equal(new[] { "2A", "2E" }, i.Handles);
        }

        /// <summary>The point parser hands references to the resolver and keeps plain numbers for the macro.</summary>
        private sealed class FakeResolver : ISnapResolver
        {
            public readonly Dictionary<string, VShape> Shapes = new Dictionary<string, VShape>();
            public int Calls;

            public double[] Resolve(Dictionary<string, object> reference, Params owner, string name)
            {
                Calls++;
                var r = SnapRules.Parse(reference, owner.Tool + "." + name);
                double[] from = r.From ?? (r.FromParam != null ? owner.Point(r.FromParam) : null);
                return SnapRules.Resolve(r, r.Of.Select(h => Shapes[h]).ToList(), from, name);
            }
        }

        [Fact]
        public void ParamsResolvesReferencesAndWritesTheCoordinatesBack()
        {
            var res = new FakeResolver();
            res.Shapes["2A"] = Line("2A", 0, 0, 4000, 0);
            var raw = new Dictionary<string, object>
            {
                ["start"] = Ref("end", "2A", J(4000, 0)),
                ["end"] = Ref("per", "2A", from: "start", offset: J(0, 900)),
                ["vertices"] = new List<object> { J(0, 0), Ref("mid", "2A") },
            };
            var p = new Params("draw_line", raw) { Snap = res };
            Near(P(4000, 0), p.Point("start"));
            Near(P(4000, 900), p.Point("end"));
            var vs = p.Points("vertices", 2);
            Near(P(2000, 0), vs[1]);
            // written back as plain [x, y]: a recorded macro replays without the original objects
            Assert.Equal(J(4000, 0), raw["start"]);
            Assert.Equal(J(2000, 0), ((List<object>)raw["vertices"])[1]);
            Assert.Equal("{\"start\":[4000,0],\"end\":[4000,900],\"vertices\":[[0,0],[2000,0]]}", JsonCodec.Serialize(raw));
        }

        [Fact]
        public void PlainPointsStillWorkAndReferencesNeedAResolver()
        {
            var p = new Params("draw_line", new Dictionary<string, object> { ["start"] = J(1, 2), ["end"] = Ref("end", "2A", J(0, 0)) });
            Assert.Equal(new[] { 1.0, 2, 0 }, p.Point("start"));
            Assert.Contains("snap references are not available", Assert.Throws<ToolException>(() => p.Point("end")).Message);
        }

        [Fact]
        public void NestedObjectsKeepTheResolver()
        {
            var res = new FakeResolver();
            res.Shapes["2A"] = Line("2A", 0, 0, 4000, 0);
            var p = new Params("zoom_window", new Dictionary<string, object>
            {
                ["window"] = new Dictionary<string, object> { ["p1"] = Ref("end", "2A", J(0, 0)) },
            }) { Snap = res };
            Near(P(0, 0), p.Obj("window").Point("p1"));
        }
    }
}
