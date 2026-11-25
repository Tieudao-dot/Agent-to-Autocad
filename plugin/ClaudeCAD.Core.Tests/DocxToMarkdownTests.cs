using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using ClaudeCAD.Plugin.Core;
using Xunit;

namespace ClaudeCAD.Core.Tests
{
    public class DocxToMarkdownTests
    {
        private const string Ns = "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" " +
                                  "xmlns:m=\"http://schemas.openxmlformats.org/officeDocument/2006/math\" " +
                                  "xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"";

        private const string Styles = "<w:styles " + Ns + ">" +
            "<w:style w:type=\"paragraph\" w:styleId=\"Heading1\"><w:name w:val=\"heading 1\"/></w:style>" +
            "<w:style w:type=\"paragraph\" w:styleId=\"Heading2\"><w:name w:val=\"heading 2\"/></w:style>" +
            // a localized style: only the outline level says it is a heading
            "<w:style w:type=\"paragraph\" w:styleId=\"TieuDe3\"><w:name w:val=\"Tiêu đề 3\"/><w:pPr><w:outlineLvl w:val=\"2\"/></w:pPr></w:style>" +
            "<w:style w:type=\"paragraph\" w:styleId=\"MyHeading\"><w:name w:val=\"My heading\"/><w:basedOn w:val=\"Heading2\"/></w:style>" +
            "</w:styles>";

        private const string Numbering = "<w:numbering " + Ns + ">" +
            "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"bullet\"/></w:lvl><w:lvl w:ilvl=\"1\"><w:numFmt w:val=\"bullet\"/></w:lvl></w:abstractNum>" +
            "<w:abstractNum w:abstractNumId=\"1\"><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"decimal\"/></w:lvl></w:abstractNum>" +
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\"/></w:num>" +
            "<w:num w:numId=\"2\"><w:abstractNumId w:val=\"1\"/></w:num>" +
            "</w:numbering>";

        private static string P(string text, string style = null, string extra = "") =>
            "<w:p>" + (style != null ? $"<w:pPr><w:pStyle w:val=\"{style}\"/></w:pPr>" : "") + R(text) + extra + "</w:p>";

        private static string R(string text, bool b = false, bool i = false) =>
            "<w:r>" + (b || i ? "<w:rPr>" + (b ? "<w:b/>" : "") + (i ? "<w:i/>" : "") + "</w:rPr>" : "") + $"<w:t xml:space=\"preserve\">{text}</w:t></w:r>";

        private static string Li(string text, int numId, int ilvl = 0) =>
            $"<w:p><w:pPr><w:numPr><w:ilvl w:val=\"{ilvl}\"/><w:numId w:val=\"{numId}\"/></w:numPr></w:pPr>{R(text)}</w:p>";

        private static string Tc(string text, string tcPr = "") => $"<w:tc>{(tcPr.Length > 0 ? "<w:tcPr>" + tcPr + "</w:tcPr>" : "")}{P(text)}</w:tc>";

        private static byte[] Docx(string body, bool styles = true, bool numbering = true)
        {
            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    void Add(string name, string xml)
                    {
                        using (var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false))) w.Write(xml);
                    }
                    Add("word/document.xml", "<w:document " + Ns + "><w:body>" + body + "</w:body></w:document>");
                    if (styles) Add("word/styles.xml", Styles);
                    if (numbering) Add("word/numbering.xml", Numbering);
                }
                return ms.ToArray();
            }
        }

        private static DocxResult Conv(string body) => DocxToMarkdown.Convert(new MemoryStream(Docx(body)));

        [Fact]
        public void HeadingsListsAndInlineFormatting()
        {
            var r = Conv(
                P("Vẽ MEP công ty A", "Heading1") +
                "<w:p>" + R("Dùng ") + R("đúng", b: true) + R(" layer ") + R("E-", i: true) + R("*") + "</w:p>" +
                P("Quy trình", "Heading2") +
                Li("Gọi get_drawing_info", 1) + Li("Kiểm layer", 1, 1) + Li("Bước một", 2) +
                P("Chi tiết", "TieuDe3") + P("Ghi chú", "MyHeading") +
                "<w:p>" + R("Dòng 1") + "<w:r><w:br/></w:r>" + R("Dòng 2") + "</w:p>" +
                "<w:p><w:hyperlink>" + R("xem tiêu chuẩn") + "</w:hyperlink></w:p>");
            Assert.Null(r.Error);
            Assert.False(r.HasFrontmatter);
            Assert.Equal("Vẽ MEP công ty A", r.FirstHeading);
            var md = r.Markdown;
            Assert.Contains("# Vẽ MEP công ty A\n", md);
            Assert.Contains("Dùng **đúng** layer *E-**", md);
            Assert.Contains("## Quy trình\n", md);
            Assert.Contains("- Gọi get_drawing_info\n  - Kiểm layer\n1. Bước một\n", md);
            Assert.Contains("### Chi tiết\n", md);
            Assert.Contains("## Ghi chú\n", md);
            Assert.Contains("Dòng 1  \nDòng 2", md);
            Assert.Contains("xem tiêu chuẩn", md);
            Assert.Empty(r.Dropped);
            Assert.DoesNotContain("\n\n\n", md);
        }

        [Fact]
        public void LayerTableFromWordParsesAsTheDrawingStandard()
        {
            var row = new Func<string[], string>(c => "<w:tr>" + string.Concat(c.Select(x => Tc(x))) + "</w:tr>");
            var r = Conv(
                "<w:p><w:r><w:t>---</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t>name: cty-a-mep</w:t></w:r></w:p>" +
                "<w:p><w:r><w:rPr><w:b/></w:rPr><w:t>description: Chuẩn vẽ MEP của công ty A - layer E-*, P-*. Dùng khi vẽ điện nước.</w:t></w:r></w:p>" +
                "<w:p><w:r><w:t>---</w:t></w:r></w:p>" +
                P("Bảng layer chuẩn", "Heading2") +
                "<w:tbl>" + row(new[] { "Layer", "Nội dung", "Màu", "Linetype", "Lineweight" }) +
                row(new[] { "E-LIGHT", "Đèn | chiếu sáng", "2", "Continuous", "0.25" }) + "</w:tbl>");
            Assert.Null(r.Error);
            Assert.True(r.HasFrontmatter);
            Assert.StartsWith("---\nname: cty-a-mep\ndescription: Chuẩn vẽ MEP", r.Markdown); // no ** in the frontmatter
            Assert.Contains("| Layer | Nội dung | Màu | Linetype | Lineweight |\n| --- | --- | --- | --- | --- |\n| `E-LIGHT` | Đèn / chiếu sáng | 2 | Continuous | 0.25 |", r.Markdown);
            var (name, desc, error) = SkillStore.ParseFrontmatter(r.Markdown);
            Assert.Null(error);
            Assert.Equal("cty-a-mep", name);
            Assert.Null(SkillStore.StandardWarning(r.Markdown));
        }

        [Fact]
        public void CountsWhatIsLeftOutAndTakesTrackedChangesAsAccepted()
        {
            var r = Conv(
                P("Tiêu đề", "Heading1") +
                "<w:p>" + R("Giữ ") + "<w:ins>" + R("thêm") + "</w:ins><w:del><w:r><w:delText>bỏ</w:delText></w:r></w:del>" + "</w:p>" +
                "<w:p><w:r><w:drawing/></w:r><w:r><w:drawing/></w:r>" + R("có ảnh") + "</w:p>" +
                "<w:p><w:r><mc:AlternateContent><mc:Choice><w:drawing><w:txbxContent>" + P("trong hộp") + "</w:txbxContent></w:drawing></mc:Choice></mc:AlternateContent></w:r></w:p>" +
                "<w:p>" + R("chú thích") + "<w:r><w:footnoteReference w:id=\"1\"/></w:r><w:r><w:commentReference w:id=\"0\"/></w:r><m:oMath/></w:p>" +
                "<w:tbl><w:tr>" + Tc("A", "<w:gridSpan w:val=\"2\"/>") + Tc("B", "<w:vMerge w:val=\"restart\"/>") + "</w:tr>" +
                "<w:tr>" + Tc("C") + Tc("D") + Tc("", "<w:vMerge/>") + "</w:tr></w:tbl>");
            Assert.Null(r.Error);
            Assert.Contains("Giữ thêm", r.Markdown);
            Assert.DoesNotContain("bỏ", r.Markdown);
            Assert.DoesNotContain("trong hộp", r.Markdown);
            Assert.Contains("| A |  | B |\n| --- | --- | --- |\n| C | D |  |", r.Markdown);
            var all = string.Join("\n", r.Dropped);
            Assert.Contains("2 ảnh / hình vẽ", all);
            Assert.Contains("1 hộp chữ", all);
            Assert.Contains("2 ô gộp", all);
            Assert.Contains("1 chú thích cuối trang", all);
            Assert.Contains("1 ghi chú lề", all);
            Assert.Contains("1 công thức", all);
        }

        [Fact]
        public void WorksWithoutStylesOrNumberingAndRefusesBadFiles()
        {
            var plain = DocxToMarkdown.Convert(new MemoryStream(Docx(P("Chỉ một dòng"), styles: false, numbering: false)));
            Assert.Null(plain.Error);
            Assert.Equal("Chỉ một dòng\n", plain.Markdown);

            Assert.NotNull(DocxToMarkdown.Convert(new MemoryStream(Encoding.UTF8.GetBytes("không phải zip"))).Error);
            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) zip.CreateEntry("x.txt");
                ms.Position = 0;
                Assert.Contains("word/document.xml", DocxToMarkdown.Convert(ms).Error);
            }
            Assert.NotNull(Conv(P("   ")).Error); // nothing usable
            Assert.Contains("chưa có dòng ---", Conv("<w:p><w:r><w:t>---</w:t></w:r></w:p>" + P("name: x")).Error);
            Assert.Contains("tối đa", Conv(P(new string('x', 45001))).Error);

            var dir = Path.Combine(Path.GetTempPath(), "ccdocx-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var doc = Path.Combine(dir, "cu.doc");
            File.WriteAllText(doc, "x");
            Assert.Contains(".docx", DocxToMarkdown.Convert(doc).Error);
            Assert.Contains("không mở được", DocxToMarkdown.Convert(Path.Combine(dir, "khong-co.docx")).Error);
        }

        [Fact]
        public void DialogInputBecomesAValidFrontmatter()
        {
            Assert.NotNull(SkillStore.CheckNewSkillInfo("", "Chuẩn vẽ của công ty A - layer, chữ, kích thước."));
            Assert.NotNull(SkillStore.CheckNewSkillInfo("cty-a", "ngắn"));
            Assert.NotNull(SkillStore.CheckNewSkillInfo("cty-a", new string('x', 1025)));
            Assert.Null(SkillStore.CheckNewSkillInfo("Chuẩn Công ty A", "Chuẩn vẽ của công ty A\n - layer, chữ, kích thước."));
            var conv = Conv(P("Chuẩn A", "Heading1") + P("Nội dung."));
            var md = SkillStore.WithFrontmatter("Chuẩn Công ty A", "Chuẩn vẽ của công ty A\n - layer, chữ, kích thước.", conv.Markdown);
            var (name, desc, error) = SkillStore.ParseFrontmatter(md);
            Assert.Null(error);
            Assert.Equal("chuan-cong-ty-a", name);
            Assert.Equal("Chuẩn vẽ của công ty A - layer, chữ, kích thước.", desc);
            Assert.Contains("# Chuẩn A", md);
        }

        [Fact]
        public void ImportTextRunsTheUsualChecks()
        {
            var root = Path.Combine(Path.GetTempPath(), "ccdocx-lib-" + Guid.NewGuid().ToString("N"));
            var store = new SkillStore(root);
            var md = "---\nname: cty-a\ndescription: Chuẩn vẽ của công ty A - layer, chữ. Dùng khi vẽ cho công ty A.\n---\n\n# A\n";
            var r = store.ImportText(md, "Chuan cong ty A", @"C:\tai lieu\chuan.docx", false);
            Assert.Equal(SkillImportStatus.Added, r.Status);
            Assert.Equal("cty-a", r.Name);
            var s = store.Scan().Single();
            Assert.Equal("You", s.Label);
            Assert.EndsWith("chuan.docx", s.Source);
            Assert.Equal(SkillImportStatus.Conflict, store.ImportText(md, "x", @"C:\b.docx", false).Status);
            Assert.Equal(SkillImportStatus.Updated, store.ImportText(md, "x", @"C:\b.docx", true).Status);
            Assert.Equal(SkillImportStatus.Invalid, store.ImportText("# không có phần đầu", "x", @"C:\c.docx", false).Status);
            // no name in the frontmatter: the suggested name (from the file) is used
            var noName = "---\ndescription: Chuẩn vẽ của công ty B - layer, chữ. Dùng khi vẽ cho công ty B.\n---\n";
            Assert.Equal("chuan-cong-ty-b", store.ImportText(noName, "Chuẩn công ty B", @"C:\d.docx", false).Name);
        }
    }
}
