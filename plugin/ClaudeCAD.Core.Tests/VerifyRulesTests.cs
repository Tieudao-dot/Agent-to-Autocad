using System;
using System.Collections.Generic;
using System.Linq;
using ClaudeCAD.Plugin.Core;
using Xunit;

namespace ClaudeCAD.Core.Tests
{
    public class VerifyRulesTests
    {
        private static int _h = 0x100;

        private static double[] P(double x, double y) => new[] { x, y };

        private static VShape Line(double x1, double y1, double x2, double y2, string layer = "A-WALL", bool turn = true) => new VShape
        {
            Handle = (_h++).ToString("X"), Type = "LINE", Layer = layer, OfTurn = turn, IsNew = turn,
            Pts = { P(x1, y1), P(x2, y2) },
            Min = P(Math.Min(x1, x2), Math.Min(y1, y2)), Max = P(Math.Max(x1, x2), Math.Max(y1, y2)),
        };

        private static VShape Poly(bool closed, string layer, params double[] xy)
        {
            var s = new VShape { Handle = (_h++).ToString("X"), Type = "POLY", Layer = layer, OfTurn = true, IsNew = true, Closed = closed };
            for (int i = 0; i < xy.Length; i += 2) s.Pts.Add(P(xy[i], xy[i + 1]));
            s.Min = P(s.Pts.Min(p => p[0]), s.Pts.Min(p => p[1]));
            s.Max = P(s.Pts.Max(p => p[0]), s.Pts.Max(p => p[1]));
            return s;
        }

        private static VShape Arc(double cx, double cy, double r, double startDeg, double endDeg, string layer = "A-DOOR") => new VShape
        {
            Handle = (_h++).ToString("X"), Type = "ARC", Layer = layer, OfTurn = true, IsNew = true,
            Center = P(cx, cy), Radius = r, StartAngle = startDeg * Math.PI / 180, EndAngle = endDeg * Math.PI / 180,
            Min = P(cx - r, cy - r), Max = P(cx + r, cy + r),
        };

        private static VShape Text(double x, double y, double w, double h, string text = "P.NGU", string layer = "A-TEXT", bool turn = true) => new VShape
        {
            Handle = (_h++).ToString("X"), Type = "TEXT", Layer = layer, OfTurn = turn, IsNew = turn, Text = text,
            Box = new List<double[]> { P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h) },
            Min = P(x, y), Max = P(x + w, y + h),
        };

        private static List<string> Codes(VReport r) => r.Issues.Select(i => i.Code).ToList();

        private static VReport Check(params VShape[] s) => VerifyRules.Check(s.ToList());

        /// <summary>A clean 4 × 3 m room: one closed polyline.</summary>
        [Fact]
        public void CleanRoomHasNoIssue()
        {
            var r = Check(Poly(true, "A-WALL", 0, 0, 4000, 0, 4000, 3000, 0, 3000));
            Assert.Empty(r.Issues);
            Assert.Equal(1, r.Checked);
        }

        [Fact]
        public void FourLinesMeetingAtCornersAreClean()
        {
            var r = Check(Line(0, 0, 4000, 0), Line(4000, 0, 4000, 3000), Line(4000, 3000, 0, 3000), Line(0, 3000, 0, 0));
            Assert.Empty(r.Issues);
        }

        [Fact]
        public void TJunctionOnTheBodyIsClean()
        {
            var r = Check(Line(0, 0, 4000, 0), Line(2000, 0, 2000, 3000));
            Assert.Empty(r.Issues);
        }

        [Fact]
        public void GapAtCornerIsFoundAndAutoFixed()
        {
            var a = Line(0, 0, 3995, 0); // 5 mm short of the corner
            var b = Line(4000, 0, 4000, 3000);
            var r = Check(a, b);
            var gap = Assert.Single(r.Issues);
            Assert.Equal("GAP", gap.Code);
            Assert.Contains("5", gap.Message);
            Assert.NotNull(gap.Fix);
            Assert.Equal(a.Handle, gap.Fix.Handle);
            Assert.Equal(1, gap.Fix.Vertex);
            Assert.Equal(4000, gap.Fix.To[0], 6);

            var after = VerifyRules.Simulate(new List<VShape> { a, b }, VerifyRules.SelectFixes(r));
            Assert.Empty(VerifyRules.Check(after).Issues);
        }

        [Fact]
        public void GapToTheBodyExtendsAlongTheLine()
        {
            var wall = Line(0, 0, 4000, 0, turn: false);
            var a = Line(2003, 3000, 2003, 4); // 4 mm short of the wall, slightly off the vertical of nothing
            var r = VerifyRules.Check(new List<VShape> { wall, a });
            var gap = Assert.Single(r.Issues);
            Assert.Equal("GAP", gap.Code);
            Assert.Equal(2003, gap.Fix.To[0], 6); // stays vertical
            Assert.Equal(0, gap.Fix.To[1], 6);
        }

        [Fact]
        public void TinyDifferencesBelowToleranceCountAsTouching()
        {
            var r = Check(Line(0, 0, 3999.995, 0), Line(4000, 0, 4000, 3000));
            Assert.Empty(r.Issues);
        }

        [Fact]
        public void GapsWiderThanTheThresholdAreIntentional()
        {
            // a 900 mm door opening between two wall lines
            var r = Check(Line(0, 0, 1000, 0), Line(1900, 0, 4000, 0));
            Assert.Empty(r.Issues);
            // just above the default 10 mm
            Assert.Empty(Check(Line(0, 0, 1000, 0), Line(1000, 11, 1000, 3000)).Issues);
        }

        [Fact]
        public void ThresholdIsConfigurable()
        {
            var shapes = new List<VShape> { Line(0, 0, 1000, 0), Line(1000, 30, 1000, 3000) };
            Assert.Empty(VerifyRules.Check(shapes).Issues);
            Assert.Equal("GAP", Assert.Single(VerifyRules.Check(shapes, new VOptions { Gap = 50 }).Issues).Code);
        }

        [Fact]
        public void GapBetweenTwoNewLinesIsReportedOnce()
        {
            var r = Check(Line(0, 0, 1000, 0), Line(1003, 0, 2000, 0));
            Assert.Equal(new[] { "GAP" }, Codes(r));
        }

        [Fact]
        public void OvershootIsFound()
        {
            var wall = Line(0, 0, 4000, 0, turn: false);
            var a = Line(2000, 3000, 2000, -6); // crosses the wall and sticks out 6 mm
            var r = VerifyRules.Check(new List<VShape> { wall, a });
            var o = Assert.Single(r.Issues);
            Assert.Equal("OVERSHOOT", o.Code);
            Assert.Null(o.Fix);
            Assert.Contains("6", o.Message);
        }

        [Fact]
        public void LongCrossingIsNotOvershoot()
        {
            var r = Check(Line(0, 0, 4000, 0), Line(2000, 3000, 2000, -500));
            Assert.Empty(r.Issues);
        }

        [Fact]
        public void OpenPolylineIsFoundAndClosed()
        {
            var p = Poly(false, "A-WALL", 0, 0, 4000, 0, 4000, 3000, 0, 3000, 0, 4);
            var r = Check(p);
            var i = Assert.Single(r.Issues);
            Assert.Equal("OPEN_POLY", i.Code);
            Assert.Equal("close", i.Fix.Kind);
            var after = VerifyRules.Simulate(new List<VShape> { p }, VerifyRules.SelectFixes(r));
            Assert.True(after[0].Closed);
            Assert.Equal(4, after[0].Pts.Count);
            Assert.Empty(VerifyRules.Check(after).Issues);
        }

        [Fact]
        public void PolylineClosedByCoincidentPointIsReported()
        {
            var r = Check(Poly(false, "A-WALL", 0, 0, 4000, 0, 4000, 3000, 0, 0));
            Assert.Equal("OPEN_POLY", Assert.Single(r.Issues).Code);
        }

        [Fact]
        public void OpenPolylineWithFarEndsIsIntentional()
        {
            Assert.Empty(Check(Poly(false, "A-WALL", 0, 0, 4000, 0, 4000, 3000)).Issues);
        }

        [Fact]
        public void ZeroLengthIsErased()
        {
            var z = Line(100, 100, 100.001, 100);
            var r = Check(z);
            var i = Assert.Single(r.Issues);
            Assert.Equal("ZERO_LEN", i.Code);
            Assert.Equal("erase", i.Fix.Kind);
        }

        [Fact]
        public void DuplicateLineIsErasedOnce()
        {
            var a = Line(0, 0, 4000, 0);
            var b = Line(4000, 0, 0, 0); // same, reversed
            var r = Check(a, b);
            var i = Assert.Single(r.Issues);
            Assert.Equal("DUPLICATE", i.Code);
            Assert.Equal(b.Handle, i.Fix.Handle);
        }

        [Fact]
        public void DuplicateOfAnExistingObjectErasesTheNewOne()
        {
            var old = Line(0, 0, 4000, 0, turn: false);
            var copy = Line(0, 0, 4000, 0);
            var r = VerifyRules.Check(new List<VShape> { old, copy });
            Assert.Equal(copy.Handle, Assert.Single(r.Issues).Fix.Handle);
        }

        [Fact]
        public void DuplicatePolylineWithShiftedStartAndReversedIsFound()
        {
            var a = Poly(true, "A-WALL", 0, 0, 4000, 0, 4000, 3000, 0, 3000);
            var b = Poly(true, "A-WALL", 4000, 3000, 4000, 0, 0, 0, 0, 3000);
            Assert.Contains("DUPLICATE", Codes(Check(a, b)));
        }

        [Fact]
        public void ParallelLinesAndOtherLayersAreNotDuplicates()
        {
            Assert.DoesNotContain(Check(Line(0, 0, 4000, 0), Line(0, 200, 4000, 200)).Issues, i => i.Code == "DUPLICATE");
            Assert.Empty(Check(Line(0, 0, 4000, 0, "A-WALL"), Line(0, 0, 4000, 0, "A-GRID")).Issues);
        }

        [Fact]
        public void DoorSwingArcTouchingLeafAndJambIsClean()
        {
            // jamb wall line ends at (1000,0); door leaf from hinge (1000,0) to (1000,900); swing from leaf end to (1900,0)
            var wall = Line(0, 0, 1000, 0, turn: false);
            var wall2 = Line(1900, 0, 4000, 0, turn: false);
            var leaf = Line(1000, 0, 1000, 900, "A-DOOR");
            var swing = Arc(1000, 0, 900, 0, 90);
            var r = VerifyRules.Check(new List<VShape> { wall, wall2, leaf, swing });
            Assert.Empty(r.Issues);
        }

        [Fact]
        public void ArcEndTouchingNothingIsReported()
        {
            var leaf = Line(1000, 0, 1000, 900, "A-DOOR");
            var swing = Arc(1000, 0, 900, 0, 90); // start (1900,0) touches nothing
            var r = VerifyRules.Check(new List<VShape> { leaf, swing });
            var i = Assert.Single(r.Issues);
            Assert.Equal("ARC_MISMATCH", i.Code);
            Assert.Equal(1900, i.At[0], 6);
        }

        [Fact]
        public void ArcEndNearALineEndIsAGapFixedOnTheLine()
        {
            var wall = Line(1903, 0, 4000, 0); // new wall line, 3 mm short of the arc end
            var leaf = Line(1000, 0, 1000, 900, "A-DOOR");
            var swing = Arc(1000, 0, 900, 0, 90);
            var r = VerifyRules.Check(new List<VShape> { leaf, swing, wall });
            var gap = Assert.Single(r.Issues);
            Assert.Equal("GAP", gap.Code);
            Assert.Equal(wall.Handle, gap.Fix.Handle); // an arc end is not moved, the line end is
            Assert.Equal(1900, gap.Fix.To[0], 6);
        }

        [Fact]
        public void Layer0IsGrouped()
        {
            var r = Check(Line(0, 0, 100, 0, "0"), Line(0, 500, 100, 500, "0"), Line(0, 1000, 100, 1000, "A-WALL"));
            var i = Assert.Single(r.Issues);
            Assert.Equal("LAYER_0", i.Code);
            Assert.Equal(2, i.Handles.Count);
            Assert.Null(i.Fix);
        }

        [Fact]
        public void OverlappingTextsAreFound()
        {
            var r = Check(Text(0, 0, 1000, 250), Text(500, 100, 1000, 250, "WC"));
            Assert.Equal("TEXT_OVERLAP", Assert.Single(r.Issues).Code);
        }

        [Fact]
        public void TextsSideBySideAreNotOverlapping()
        {
            Assert.Empty(Check(Text(0, 0, 1000, 250), Text(1000, 0, 1000, 250, "WC")).Issues);
            // boxes that only graze each other (less than 10 % of the height) are fine
            Assert.Empty(Check(Text(0, 0, 1000, 250), Text(0, 240, 1000, 250, "WC")).Issues);
        }

        [Fact]
        public void RotatedTextBoxesUseTheirRealShape()
        {
            // two 45° texts whose axis-aligned boxes overlap but the texts do not
            var a = new VShape { Handle = "A1", Type = "TEXT", Layer = "A-TEXT", OfTurn = true, Text = "A", Box = new List<double[]> { P(0, 0), P(707, 707), P(636, 778), P(-71, 71) } };
            var b = new VShape { Handle = "A2", Type = "TEXT", Layer = "A-TEXT", OfTurn = true, Text = "B", Box = new List<double[]> { P(400, 0), P(1107, 707), P(1036, 778), P(329, 71) } };
            Assert.Empty(Check(a, b).Issues);
        }

        [Fact]
        public void StrayObjectIsFound()
        {
            var r = Check(Line(0, 0, 4000, 0), Line(4000, 0, 4000, 3000), Line(4000, 3000, 0, 3000), Line(0, 3000, 0, 0),
                Text(1500, 1500, 1000, 250), Text(400000, 1500, 1000, 250, "WC"));
            var i = Assert.Single(r.Issues);
            Assert.Equal("STRAY", i.Code);
        }

        [Fact]
        public void CountComparesNewObjectsByLayer()
        {
            var shapes = new List<VShape> { Poly(true, "A-WALL", 0, 0, 4000, 0, 4000, 3000, 0, 3000), Text(1500, 1500, 1000, 250) };
            var ok = VerifyRules.Check(shapes, new VOptions { Expect = new Dictionary<string, int> { ["A-WALL"] = 1, ["a-text"] = 1 } });
            Assert.Empty(ok.Issues);
            var bad = VerifyRules.Check(shapes, new VOptions { Expect = new Dictionary<string, int> { ["A-WALL"] = 1, ["A-DOOR"] = 2 } });
            Assert.Equal(2, bad.Issues.Count(i => i.Code == "COUNT")); // A-DOOR missing, A-TEXT not planned
            Assert.Contains(bad.Issues, i => i.Message.Contains("A-DOOR") && i.Message.Contains("2"));
        }

        [Fact]
        public void NeighboursAreNotChecked()
        {
            var r = VerifyRules.Check(new List<VShape> { Line(0, 0, 100, 0, "0", turn: false), Line(103, 0, 500, 0, "0", turn: false) });
            Assert.Empty(r.Issues);
            Assert.Equal(0, r.Checked);
        }

        [Fact]
        public void IgnoredCodesAreLeftOut()
        {
            var shapes = new List<VShape> { Text(0, 0, 1000, 250), Text(500, 100, 1000, 250, "WC") };
            Assert.Empty(VerifyRules.Check(shapes, new VOptions { Ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "text_overlap" } }).Issues);
        }

        [Fact]
        public void ReportIsCappedWithOmittedCounts()
        {
            var shapes = Enumerable.Range(0, 40).Select(i => Line(i * 1000, 0, i * 1000 + 500, 0, "0")).ToList();
            shapes.AddRange(Enumerable.Range(0, 40).Select(i => Line(i * 1000, 5000, i * 1000 + 0.001, 5000)));
            var r = VerifyRules.Check(shapes, new VOptions { MaxIssues = 30 });
            Assert.Equal(30, r.Issues.Count);
            Assert.Equal(41, r.Total); // 40 ZERO_LEN + 1 LAYER_0 group
            Assert.True(r.Omitted.Values.Sum() == 11);
        }

        [Fact]
        public void FixesDoNotConflict()
        {
            var a = Line(0, 0, 3995, 0);
            var b = Line(4000, 0, 4000, 3000);
            var dupA = Line(0, 0, 3995, 0);
            var r = Check(a, b, dupA);
            var fixes = VerifyRules.SelectFixes(r);
            Assert.Single(fixes, f => f.Kind == "erase");
            // the erased copy is never also moved
            Assert.DoesNotContain(fixes, f => f.Kind == "move_vertex" && f.Handle == fixes.First(e => e.Kind == "erase").Handle);
            var after = VerifyRules.Simulate(new List<VShape> { a, b, dupA }, fixes);
            Assert.Empty(VerifyRules.Check(after).Issues);
        }

        [Fact]
        public void BulgedPolylineSegmentsAreArcs()
        {
            // a polyline with a half-circle segment from (0,0) to (2000,0) bulging below (CCW from a to b), then a line
            var p = Poly(false, "A-WALL", 0, 0, 2000, 0, 2000, 1000);
            p.Bulges = new List<double> { 1, 0, 0 };
            // a line ending exactly on the bottom of the half circle: (1000,-1000)
            var l = Line(1000, -3000, 1000, -1000);
            Assert.Empty(VerifyRules.Check(new List<VShape> { p, l }).Issues);
        }

        [Fact]
        public void SelfCheckSummaryIsKeptInTheChatHistory()
        {
            var r = Check(Line(0, 0, 100, 0, "0"), Text(0, 0, 1000, 250), Text(500, 100, 1000, 250, "WC"));
            var sum = SelfCheckSummary.From(r, 2);
            Assert.False(sum.Ok);
            Assert.Equal("Tự kiểm: 3 đối tượng · 2 lỗi ✗ · đã tự sửa 2", sum.Headline);
            var t = new ChatTranscript();
            t.Entries.Add(new ChatEntry { Role = "assistant", Text = "x", SelfCheck = sum });
            t.Entries.Add(new ChatEntry { Role = "assistant", Text = "y" });
            var back = ChatTranscript.FromJson(t.ToJson());
            var s = back.Entries[0].SelfCheck;
            Assert.NotNull(s);
            Assert.Equal(sum.Headline, s.Headline);
            Assert.Equal(sum.AllHandles(), s.AllHandles());
            Assert.Null(back.Entries[1].SelfCheck);
            Assert.Contains("Tự kiểm: 3 đối tượng", back.ToMarkdown());
            var clean = new SelfCheckSummary { Checked = 5, Stale = true };
            Assert.Equal("Tự kiểm: 5 đối tượng · không lỗi ✓ · có sửa sau lần kiểm", clean.Headline);
        }

        [Fact]
        public void EveryCodeHasALabel()
        {
            foreach (var c in VerifyRules.Codes) Assert.NotEqual(c, VerifyRules.Label(c));
        }
    }
}
