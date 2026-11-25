using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>
    /// Skill Markdown → .docx so "Sửa ▸ Mở bằng Word" can open a skill in Word (v0.9.8,
    /// docs/ke-hoach-sua-skill-mo-bang.md); <see cref="DocxToMarkdown"/> brings it back. Writes the small subset skills
    /// use: headings, paragraphs (line breaks), bulleted / numbered lists with levels, tables, **bold**, *italic*,
    /// `code` (kept with its backticks so it survives the way back). The frontmatter is not written: the plugin keeps it.
    /// </summary>
    public static class MarkdownToDocx
    {
        private static readonly Regex Heading = new Regex(@"^(#{1,6})\s+(.*)$");
        private static readonly Regex Bullet = new Regex(@"^(\s*)[-*+]\s+(.*)$");
        private static readonly Regex Numbered = new Regex(@"^(\s*)\d+[.)]\s+(.*)$");
        private static readonly Regex TableRow = new Regex(@"^\s*\|.*\|\s*$");
        private static readonly Regex TableRule = new Regex(@"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$");

        public static byte[] Convert(string markdownBody)
        {
            var body = new StringBuilder();
            var lines = (markdownBody ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var para = new List<string>();
            void Flush()
            {
                if (para.Count == 0) return;
                body.Append(Paragraph(null, string.Join("\n", para), null, 0));
                para.Clear();
            }
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Trim().Length == 0) { Flush(); continue; }
                Match m;
                if ((m = Heading.Match(line)).Success)
                {
                    Flush();
                    body.Append(Paragraph("Heading" + m.Groups[1].Value.Length, m.Groups[2].Value.Trim(), null, 0));
                }
                else if (TableRow.IsMatch(line) && i + 1 < lines.Length && TableRule.IsMatch(lines[i + 1]))
                {
                    Flush();
                    var rows = new List<List<string>> { Cells(line) };
                    i += 2;
                    while (i < lines.Length && TableRow.IsMatch(lines[i])) rows.Add(Cells(lines[i++]));
                    i--;
                    body.Append(Table(rows));
                }
                else if ((m = Bullet.Match(line)).Success && !Regex.IsMatch(line.Trim(), @"^[-*_](\s*[-*_]){2,}$"))
                {
                    Flush();
                    body.Append(Paragraph(null, m.Groups[2].Value, "1", Level(m.Groups[1].Value)));
                }
                else if ((m = Numbered.Match(line)).Success)
                {
                    Flush();
                    body.Append(Paragraph(null, m.Groups[2].Value, "2", Level(m.Groups[1].Value)));
                }
                else para.Add(line.EndsWith("  ") ? line.TrimEnd() : line.Trim());
            }
            Flush();
            return Package(body.ToString());
        }

        private static int Level(string indent) => Math.Min(8, indent.Replace("\t", "  ").Length / 2);

        private static List<string> Cells(string row)
        {
            var t = row.Trim();
            if (t.StartsWith("|")) t = t.Substring(1);
            if (t.EndsWith("|")) t = t.Substring(0, t.Length - 1);
            return t.Split('|').Select(c => c.Trim()).ToList();
        }

        private const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        private static string Paragraph(string style, string text, string numId, int level)
        {
            var sb = new StringBuilder("<w:p>");
            if (style != null || numId != null)
            {
                sb.Append("<w:pPr>");
                if (style != null) sb.Append($"<w:pStyle w:val=\"{style}\"/>");
                if (numId != null) sb.Append($"<w:numPr><w:ilvl w:val=\"{level}\"/><w:numId w:val=\"{numId}\"/></w:numPr>");
                sb.Append("</w:pPr>");
            }
            sb.Append(Runs(text));
            return sb.Append("</w:p>").ToString();
        }

        /// <summary>**bold**, *italic*, ***both***; "  \n" / "&lt;br&gt;" / "\n" = line break.</summary>
        private static string Runs(string text)
        {
            var sb = new StringBuilder();
            var parts = Regex.Split(text.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("  \n", "\n"), @"(\*\*\*[^*\n]+\*\*\*|\*\*[^*\n]+\*\*|(?<![*\w])\*(?=[\p{L}\p{N}])[^*\n]*?(?<=[\p{L}\p{N}.)])\*(?![*\w]))");
            foreach (var part in parts)
            {
                if (part.Length == 0) continue;
                bool b = false, i = false;
                var t = part;
                if (t.StartsWith("***") && t.EndsWith("***") && t.Length > 6) { b = i = true; t = t.Substring(3, t.Length - 6); }
                else if (t.StartsWith("**") && t.EndsWith("**") && t.Length > 4) { b = true; t = t.Substring(2, t.Length - 4); }
                else if (t.StartsWith("*") && t.EndsWith("*") && t.Length > 2 && !t.StartsWith("**")) { i = true; t = t.Substring(1, t.Length - 2); }
                var lines = t.Split('\n');
                for (int k = 0; k < lines.Length; k++)
                {
                    sb.Append("<w:r>");
                    if (b || i) sb.Append("<w:rPr>").Append(b ? "<w:b/>" : "").Append(i ? "<w:i/>" : "").Append("</w:rPr>");
                    if (k > 0) sb.Append("<w:br/>");
                    sb.Append("<w:t xml:space=\"preserve\">").Append(SecurityElement.Escape(lines[k])).Append("</w:t></w:r>");
                }
            }
            return sb.ToString();
        }

        private static string Table(List<List<string>> rows)
        {
            int cols = rows.Max(r => r.Count);
            var sb = new StringBuilder("<w:tbl><w:tblPr><w:tblStyle w:val=\"TableGrid\"/><w:tblW w:w=\"0\" w:type=\"auto\"/>" +
                "<w:tblBorders><w:top w:val=\"single\" w:sz=\"4\"/><w:left w:val=\"single\" w:sz=\"4\"/><w:bottom w:val=\"single\" w:sz=\"4\"/>" +
                "<w:right w:val=\"single\" w:sz=\"4\"/><w:insideH w:val=\"single\" w:sz=\"4\"/><w:insideV w:val=\"single\" w:sz=\"4\"/></w:tblBorders></w:tblPr><w:tblGrid>");
            for (int c = 0; c < cols; c++) sb.Append("<w:gridCol/>");
            sb.Append("</w:tblGrid>");
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append("<w:tr>");
                for (int c = 0; c < cols; c++)
                {
                    var text = c < rows[r].Count ? rows[r][c] : "";
                    // the Layer column comes back wrapped in `…` (DocxToMarkdown): show the plain name in Word
                    if (text.Length > 2 && text.StartsWith("`") && text.EndsWith("`") && text.IndexOf('`', 1) == text.Length - 1) text = text.Substring(1, text.Length - 2);
                    // header kept plain (no bold): "**Layer**" would not read back as the header DrawingStandard expects
                    sb.Append("<w:tc>").Append(Paragraph(null, text, null, 0)).Append("</w:tc>");
                }
                sb.Append("</w:tr>");
            }
            return sb.Append("</w:tbl><w:p/>").ToString();
        }

        private static byte[] Package(string body)
        {
            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    void Add(string name, string xml)
                    {
                        using (var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false))) w.Write(xml);
                    }
                    Add("[Content_Types].xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                        "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
                        "<Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/>" +
                        "<Override PartName=\"/word/numbering.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml\"/>" +
                        "</Types>");
                    Add("_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
                    Add("word/_rels/document.xml.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering\" Target=\"numbering.xml\"/></Relationships>");
                    Add("word/document.xml",
                        $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"{W}\"><w:body>{body}" +
                        "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"1134\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1134\"/></w:sectPr></w:body></w:document>");
                    var styles = new StringBuilder($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:styles xmlns:w=\"{W}\">" +
                        "<w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii=\"Arial\" w:hAnsi=\"Arial\" w:cs=\"Arial\"/><w:sz w:val=\"22\"/></w:rPr></w:rPrDefault></w:docDefaults>" +
                        "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/><w:pPr><w:spacing w:after=\"120\"/></w:pPr></w:style>");
                    int[] sizes = { 32, 28, 26, 24, 22, 22 };
                    for (int h = 1; h <= 6; h++)
                        styles.Append($"<w:style w:type=\"paragraph\" w:styleId=\"Heading{h}\"><w:name w:val=\"heading {h}\"/><w:basedOn w:val=\"Normal\"/>" +
                            $"<w:pPr><w:keepNext/><w:spacing w:before=\"240\" w:after=\"120\"/><w:outlineLvl w:val=\"{h - 1}\"/></w:pPr><w:rPr><w:b/><w:sz w:val=\"{sizes[h - 1]}\"/></w:rPr></w:style>");
                    styles.Append("<w:style w:type=\"table\" w:styleId=\"TableGrid\"><w:name w:val=\"Table Grid\"/></w:style></w:styles>");
                    Add("word/styles.xml", styles.ToString());
                    var num = new StringBuilder($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:numbering xmlns:w=\"{W}\">");
                    foreach (var (id, fmt) in new[] { ("0", "bullet"), ("1", "decimal") })
                    {
                        num.Append($"<w:abstractNum w:abstractNumId=\"{id}\">");
                        for (int l = 0; l < 9; l++)
                            num.Append($"<w:lvl w:ilvl=\"{l}\"><w:start w:val=\"1\"/><w:numFmt w:val=\"{fmt}\"/><w:lvlText w:val=\"{(fmt == "bullet" ? "•" : "%" + (l + 1) + ".")}\"/>" +
                                $"<w:lvlJc w:val=\"left\"/><w:pPr><w:ind w:left=\"{360 * (l + 1)}\" w:hanging=\"360\"/></w:pPr></w:lvl>");
                        num.Append("</w:abstractNum>");
                    }
                    num.Append("<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\"/></w:num><w:num w:numId=\"2\"><w:abstractNumId w:val=\"1\"/></w:num></w:numbering>");
                    Add("word/numbering.xml", num.ToString());
                }
                return ms.ToArray();
            }
        }
    }
}
