using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClaudeCAD.Plugin.Core;
using Xunit;

namespace ClaudeCAD.Core.Tests
{
    /// <summary>The machine-readable tables of SKILL.md (layer B) and the 4 verify codes that use them.</summary>
    public class DrawingStandardTests
    {
        private static string RepoSkill(string name)
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "skills", name, "SKILL.md"))) d = d.Parent;
            Assert.NotNull(d);
            return File.ReadAllText(Path.Combine(d.FullName, "skills", name, "SKILL.md"));
        }

        [Theory]
        [InlineData("ve-kien-truc", "A-WALL", 7, "CONTINUOUS", 0.50)]
        [InlineData("ve-mep", "P-SUPL", 5, "CONTINUOUS", 0.35)]
        [InlineData("ve-ha-tang", "C-ROAD-CL", 1, "CENTER", 0.25)]
        public void TheThreeSampleSkillsAreReadableWithoutProblems(string skill, string layer, int color, string lt, double lw)
        {
            var problems = new List<string>();
            var std = DrawingStandard.Parse(RepoSkill(skill), skill, problems);
            Assert.Empty(problems);
            Assert.True(std.Layers.Count >= 10, $"{skill}: {std.Layers.Count} layers");
            var row = std.Layers[layer];
            Assert.Equal(color, row.Color);
            Assert.Equal(lt, row.Linetype);
            Assert.Equal(lw, row.Lineweight);
            Assert.Equal(10, std.GapMm);
        }

        [Fact]
        public void ArchitectureTextTableGivesHeightsPerScale()
        {
            var std = DrawingStandard.Parse(RepoSkill("ve-kien-truc"), "ve-kien-truc");
            Assert.Equal(new[] { 250.0, 350 }, std.TextHeights(100));
            Assert.Equal(2.5, std.DimTextMm);
            Assert.Equal(250, std.DefaultTextHeight(100)); // "Diện tích, ghi chú"
            Assert.Equal(125, std.DefaultTextHeight(50));
            Assert.True(std.TextHeightOk(350, 100));
            Assert.True(std.TextHeightOk(351, 100)); // within 1 %
            Assert.False(std.TextHeightOk(300, 100));
        }

        [Fact]
        public void BrokenTablesAreReportedLineByLine()
        {
            var md = "# X\n\n## 2. Bảng layer chuẩn\n\n| Layer | Nội dung | Màu | Linetype | Lineweight |\n| --- | --- | --- | --- | --- |\n" +
                     "| `A-WALL` | tường | 7 | CONTINUOUS | 0.50 |\n| A-DOOR | cửa | 3 | CONTINUOUS | 0.25 |\n| `A-DIM` | dim | vàng | CONTINUOUS | dày |\n\n" +
                     "## 4. Chữ và kích thước theo tỷ lệ in\n\n| Loại | Trên giấy |\n| --- | --- |\n| Tên phòng | to |\n\nNgưỡng khe hở: abc mm\n";
            var problems = new List<string>();
            var std = DrawingStandard.Parse(md, "x", problems);
            Assert.Contains(problems, p => p.Contains("dòng 8") && p.Contains("dấu"));
            Assert.Contains(problems, p => p.Contains("dòng 9") && p.Contains("màu"));
            Assert.Contains(problems, p => p.Contains("dòng 9") && p.Contains("lineweight"));
            Assert.Contains(problems, p => p.Contains("Trên giấy"));
            Assert.Equal(new[] { "A-WALL", "A-DIM" }, std.Layers.Keys);
            Assert.Null(std.GapMm);
        }

        [Fact]
        public void ASkillWithoutTablesHasNoStandardAndNoProblems()
        {
            var problems = new List<string>();
            var std = DrawingStandard.Parse("---\nname: x\n---\n# Ghi chú\nkhông có bảng\n", "x", problems);
            Assert.True(std.IsEmpty);
            Assert.Empty(problems);
        }

        [Fact]
        public void GapCanBeOverriddenWithAComma()
        {
            Assert.Equal(2.5, DrawingStandard.Parse("Ngưỡng khe hở: 2,5 mm", "x").GapMm);
        }

        [Fact]
        public void MergeKeepsTheFirstSkillOnConflicts()
        {
            var a = DrawingStandard.Parse(RepoSkill("ve-kien-truc"), "a");
            var b = DrawingStandard.Parse(RepoSkill("ve-mep"), "b");
            var m = DrawingStandard.Merge(new[] { a, b });
            Assert.True(m.Layers.ContainsKey("A-WALL") && m.Layers.ContainsKey("E-LIGHT"));
            Assert.Equal(new[] { "a", "b" }, m.Sources);
            Assert.Equal(2.5, m.DimTextMm);
        }

        [Theory]
        [InlineData("1:100", 100.0)]
        [InlineData("1/50", 50.0)]
        [InlineData(" 200 ", 200.0)]
        public void ScaleDenominatorReadsThePaletteScale(string s, double d) => Assert.Equal(d, DrawingStandard.ScaleDenominator(s));

        [Fact]
        public void UnknownScaleIsNull()
        {
            Assert.Null(DrawingStandard.ScaleDenominator(""));
            Assert.Null(DrawingStandard.ScaleDenominator("NTS"));
        }

        [Fact]
        public void UserNamedLayerIsAWholeWord()
        {
            Assert.True(DrawingStandard.TextNamesLayer("vẽ 3 đường lên layer xyz-tam", "XYZ-TAM"));
            Assert.True(DrawingStandard.TextNamesLayer("chuyển sang @TAM nhé", "TAM"));
            Assert.False(DrawingStandard.TextNamesLayer("vẽ lên layer XYZ-TAM2", "XYZ-TAM"));
            Assert.False(DrawingStandard.TextNamesLayer("vẽ tường", "A-WALL"));
        }

        // ------------------------------------------------------------------ verify codes using the standard

        private static VOptions Opts(Func<string, bool> allowed = null) => new VOptions
        {
            Standard = DrawingStandard.Parse(RepoSkill("ve-kien-truc"), "ve-kien-truc"),
            Scale = 100,
            LayerAllowed = allowed,
        };

        private static VShape Line(string h, string layer) => new VShape
        {
            Handle = h, Type = "LINE", Layer = layer, OfTurn = true, IsNew = true,
            Pts = { new[] { 0.0, Convert.ToInt32(h, 16) * 1000.0 }, new[] { 500.0, Convert.ToInt32(h, 16) * 1000.0 } },
        };

        private static VShape Txt(string h, double height) => new VShape { Handle = h, Type = "TEXT", Layer = "A-TEXT", OfTurn = true, TextHeight = height };

        [Fact]
        public void LayerOutsideTheStandardIsReportedUnlessTheUserAskedForIt()
        {
            var shapes = new List<VShape> { Line("1", "A-WALL"), Line("2", "XREF-TAM"), Line("3", "XREF-TAM") };
            var i = Assert.Single(VerifyRules.Check(shapes, Opts()).Issues);
            Assert.Equal("LAYER_UNKNOWN", i.Code);
            Assert.Equal(new[] { "2", "3" }, i.Handles);
            Assert.Contains("A-", i.Hint);
            Assert.Empty(VerifyRules.Check(shapes, Opts(l => l == "XREF-TAM")).Issues);
            // without a skill table: no LAYER_UNKNOWN
            Assert.Empty(VerifyRules.Check(shapes).Issues);
        }

        [Fact]
        public void PropertiesSetOnTheObjectAreReportedButNotOnHatches()
        {
            var a = Line("1", "A-WALL"); a.ColorOverride = true;
            var b = Line("2", "A-WALL"); b.LineweightOverride = true;
            var h = new VShape { Handle = "3", Type = "HATCH", Layer = "A-COL", OfTurn = true, ColorOverride = true };
            var i = Assert.Single(VerifyRules.Check(new List<VShape> { a, b, h }, Opts()).Issues);
            Assert.Equal("PROP_OVERRIDE", i.Code);
            Assert.Equal(2, i.Handles.Count);
            Assert.Contains("màu", i.Message);
            Assert.Contains("lineweight", i.Message);
        }

        [Fact]
        public void TextHeightMustMatchTheTableAtThePrintScale()
        {
            var shapes = new List<VShape> { Txt("1", 350), Txt("2", 250), Txt("3", 300), Txt("4", 300) };
            var i = Assert.Single(VerifyRules.Check(shapes, Opts()).Issues);
            Assert.Equal("TEXT_HEIGHT", i.Code);
            Assert.Equal(new[] { "3", "4" }, i.Handles);
            Assert.Contains("250 / 350", i.Message);
            // unknown print scale: not checked
            var o = Opts(); o.Scale = null;
            Assert.Empty(VerifyRules.Check(shapes, o).Issues);
        }

        [Fact]
        public void DimensionsMustUseTheScaleAndTextHeightOfTheSkill()
        {
            var ok = new VShape { Handle = "1", Type = "DIM", Layer = "A-DIM", OfTurn = true, DimScale = 100, DimTextHeight = 2.5 };
            var badScale = new VShape { Handle = "2", Type = "DIM", Layer = "A-DIM", OfTurn = true, DimScale = 50, DimTextHeight = 2.5 };
            var badText = new VShape { Handle = "3", Type = "DIM", Layer = "A-DIM", OfTurn = true, DimScale = 100, DimTextHeight = 3.5 };
            var issues = VerifyRules.Check(new List<VShape> { ok, badScale, badText }, Opts()).Issues;
            Assert.Equal(2, issues.Count(x => x.Code == "DIM_STYLE"));
            Assert.Contains(issues, x => x.Handles.SequenceEqual(new[] { "2" }) && x.Message.Contains("scale 50"));
        }

        [Fact]
        public void NewCodesHaveLabels()
        {
            foreach (var c in new[] { "LAYER_UNKNOWN", "PROP_OVERRIDE", "TEXT_HEIGHT", "DIM_STYLE" })
            {
                Assert.Contains(c, VerifyRules.Codes);
                Assert.NotEqual(c, VerifyRules.Label(c));
            }
            Assert.Equal(14, VerifyRules.Codes.Length);
        }
    }
}
