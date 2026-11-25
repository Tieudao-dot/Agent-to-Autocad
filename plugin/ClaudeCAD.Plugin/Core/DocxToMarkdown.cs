using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>A Word file turned into skill Markdown, with what had to be left out.</summary>
    public sealed class DocxResult
    {
        public string Markdown = "";
        /// <summary>The file starts with its own frontmatter (paragraphs "---", "name: …", "description: …", "---").</summary>
        public bool HasFrontmatter;
        /// <summary>First heading of the document (suggested name / title), or null.</summary>
        public string FirstHeading;
        /// <summary>Lines for the user: "3 ảnh / hình vẽ", "2 ô gộp …" — empty when nothing was lost.</summary>
        public readonly List<string> Dropped = new List<string>();
        public string Error; // the file cannot become a skill (shown instead of adding it)
    }

    /// <summary>
    /// .docx → Markdown for "+ Add skill" (v0.9.8, docs/ke-hoach-skill-docx-gdocs.md). Reads word/document.xml with
    /// styles.xml / numbering.xml straight from the zip (no Word, no extra package): headings, bold / italic,
    /// bulleted and numbered lists with levels, tables (first row = header, so a "Bảng layer chuẩn" written in Word
    /// still parses), link text, line breaks; tracked changes as accepted. Images, text boxes, footnotes, comments
    /// and equations are left out and counted; merged cells are split with the extra cells empty.
    /// </summary>
    public static class DocxToMarkdown
    {
        public const long MaxFileBytes = 10L * 1024 * 1024;
        public const int MaxChars = SkillPackLogic.MaxSkillChars;

        private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly XNamespace M = "http://schemas.openxmlformats.org/officeDocument/2006/math";

        public static DocxResult Convert(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (string.Equals(info.Extension, ".doc", StringComparison.OrdinalIgnoreCase))
                    return Fail("file .doc (Word cũ) chưa đọc được — mở bằng Word rồi lưu lại thành .docx");
                if (info.Length > MaxFileBytes) return Fail($"file nặng {info.Length / 1024 / 1024} MB, tối đa {MaxFileBytes / 1024 / 1024} MB");
                using (var fs = File.OpenRead(path)) return Convert(fs);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return Fail("không mở được file: " + ex.Message);
            }
        }

        public static DocxResult Convert(Stream stream)
        {
            XDocument doc, styles = null, numbering = null;
            try
            {
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
                {
                    doc = Load(zip, "word/document.xml");
                    if (doc == null) return Fail("không phải file Word .docx (thiếu word/document.xml)");
                    styles = Load(zip, "word/styles.xml");
                    numbering = Load(zip, "word/numbering.xml");
                }
            }
            catch (InvalidDataException) { return Fail("không phải file Word .docx (file hỏng hoặc không phải .docx)"); }
            catch (System.Xml.XmlException) { return Fail("nội dung file Word bị hỏng"); }

            var c = new Converter(styles, numbering);
            var body = doc.Root?.Element(W + "body");
            if (body == null) return Fail("file Word trống");
            c.Blocks(body.Elements());
            return c.Finish();
        }

        private static DocxResult Fail(string why) => new DocxResult { Error = why };

        private static XDocument Load(ZipArchive zip, string name)
        {
            var e = zip.GetEntry(name);
            if (e == null) return null;
            using (var s = e.Open()) return XDocument.Load(s);
        }

        private sealed class Converter
        {
            private readonly Dictionary<string, int> _headingStyles = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> _numFormats = new Dictionary<string, string>(); // "numId|ilvl" -> numFmt
            private readonly StringBuilder _out = new StringBuilder();
            private readonly DocxResult _r = new DocxResult();
            private bool _lastWasList;
            private bool _started; // something (other than empty paragraphs) was written
            private bool _inFrontmatter;
            private bool _count = true; // false while probing a paragraph for frontmatter
            private int _images, _merged, _footnotes, _comments, _equations, _textboxes, _nested;

            public Converter(XDocument styles, XDocument numbering)
            {
                ReadStyles(styles);
                ReadNumbering(numbering);
            }

            private void ReadStyles(XDocument styles)
            {
                var all = styles?.Root?.Elements(W + "style").ToList() ?? new List<XElement>();
                var basedOn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in all)
                {
                    var id = (string)s.Attribute(W + "styleId");
                    if (id == null) continue;
                    var name = ((string)s.Element(W + "name")?.Attribute(W + "val") ?? "").Trim().ToLowerInvariant();
                    var outline = (string)s.Element(W + "pPr")?.Element(W + "outlineLvl")?.Attribute(W + "val");
                    int level = 0;
                    if (name == "title") level = 1;
                    else if (name.StartsWith("heading ") && int.TryParse(name.Substring(8), out var n)) level = n;
                    else if (int.TryParse(outline, out var o) && o >= 0 && o < 9) level = o + 1; // localized names (Tiêu đề 1…)
                    if (level > 0) _headingStyles[id] = Math.Min(level, 6);
                    var b = (string)s.Element(W + "basedOn")?.Attribute(W + "val");
                    if (b != null) basedOn[id] = b;
                }
                foreach (var id in basedOn.Keys.ToList())
                {
                    if (_headingStyles.ContainsKey(id)) continue;
                    var cur = id;
                    for (int i = 0; i < 10 && basedOn.TryGetValue(cur, out var parent); i++)
                    {
                        if (_headingStyles.TryGetValue(parent, out var lv)) { _headingStyles[id] = lv; break; }
                        cur = parent;
                    }
                }
            }

            private void ReadNumbering(XDocument numbering)
            {
                var root = numbering?.Root;
                if (root == null) return;
                var abstracts = new Dictionary<string, XElement>();
                foreach (var a in root.Elements(W + "abstractNum"))
                {
                    var id = (string)a.Attribute(W + "abstractNumId");
                    if (id != null) abstracts[id] = a;
                }
                foreach (var num in root.Elements(W + "num"))
                {
                    var numId = (string)num.Attribute(W + "numId");
                    var absId = (string)num.Element(W + "abstractNumId")?.Attribute(W + "val");
                    if (numId == null || absId == null || !abstracts.TryGetValue(absId, out var abs)) continue;
                    foreach (var lvl in abs.Elements(W + "lvl"))
                        _numFormats[numId + "|" + (string)lvl.Attribute(W + "ilvl")] = (string)lvl.Element(W + "numFmt")?.Attribute(W + "val") ?? "bullet";
                }
            }

            public void Blocks(IEnumerable<XElement> elements)
            {
                foreach (var e in elements)
                {
                    if (e.Name == W + "p") Paragraph(e);
                    else if (e.Name == W + "tbl") Table(e);
                    else if (e.Name == W + "sdt") Blocks(e.Element(W + "sdtContent")?.Elements() ?? Enumerable.Empty<XElement>());
                    else if (e.Name == W + "customXml") Blocks(e.Elements());
                }
            }

            private void Paragraph(XElement p)
            {
                var pPr = p.Element(W + "pPr");
                // frontmatter typed at the top of the Word file: read as plain text, nothing counted
                if (!_started || _inFrontmatter)
                {
                    _count = false;
                    var probe = Inline(p, plain: true).Trim();
                    _count = true;
                    if (!_inFrontmatter && probe == "---") { _inFrontmatter = true; _out.Append("---\n"); return; }
                    if (_inFrontmatter)
                    {
                        _out.Append(probe).Append('\n');
                        if (probe == "---") { _inFrontmatter = false; _started = true; _r.HasFrontmatter = true; _out.Append('\n'); }
                        return;
                    }
                }
                var text = Inline(p, plain: false);
                var trimmed = text.Trim();
                if (trimmed.Length == 0) { _lastWasList = false; return; }

                var style = (string)pPr?.Element(W + "pStyle")?.Attribute(W + "val");
                var outline = (string)pPr?.Element(W + "outlineLvl")?.Attribute(W + "val");
                int heading = 0;
                if (style != null && _headingStyles.TryGetValue(style, out var lv)) heading = lv;
                else if (int.TryParse(outline, out var o) && o >= 0 && o < 6) heading = o + 1;

                var numPr = pPr?.Element(W + "numPr");
                var numId = (string)numPr?.Element(W + "numId")?.Attribute(W + "val");
                if (heading > 0)
                {
                    EndList();
                    var h = Strip(trimmed);
                    _out.Append(new string('#', heading)).Append(' ').Append(h).Append("\n\n");
                    if (_r.FirstHeading == null) _r.FirstHeading = h;
                    _lastWasList = false;
                }
                else if (numId != null && numId != "0")
                {
                    int ilvl = int.TryParse((string)numPr.Element(W + "ilvl")?.Attribute(W + "val"), out var il) ? Math.Max(0, Math.Min(il, 8)) : 0;
                    _numFormats.TryGetValue(numId + "|" + ilvl, out var fmt);
                    if (!_lastWasList && _out.Length > 0 && !EndsWithBlank()) _out.Append('\n');
                    _out.Append(new string(' ', ilvl * 2)).Append(fmt == null || fmt == "bullet" || fmt == "none" ? "- " : "1. ")
                        .Append(trimmed.Replace("\n", " ")).Append('\n');
                    _lastWasList = true;
                }
                else
                {
                    EndList();
                    _out.Append(trimmed.Replace("\n", "  \n")).Append("\n\n");
                    _lastWasList = false;
                }
                _started = true;
            }

            private void EndList()
            {
                if (_lastWasList) _out.Append('\n');
            }

            private bool EndsWithBlank() => _out.Length >= 2 && _out[_out.Length - 1] == '\n' && _out[_out.Length - 2] == '\n';

            private static string Strip(string s) => s.Replace("**", "").Replace("*", "").Trim();

            /// <summary>Text of a paragraph (or cell paragraph) with **bold** / *italic*; counts what is left out.</summary>
            private string Inline(XElement p, bool plain)
            {
                var runs = new List<(string text, bool b, bool i)>();
                Collect(p, runs, plain);
                var sb = new StringBuilder();
                int k = 0;
                while (k < runs.Count)
                {
                    var (t, b, i) = runs[k];
                    var merged = new StringBuilder(t);
                    while (k + 1 < runs.Count && runs[k + 1].b == b && runs[k + 1].i == i) { k++; merged.Append(runs[k].text); }
                    var s = merged.ToString();
                    if ((b || i) && s.Trim().Length > 0 && !plain)
                    {
                        var lead = s.Length - s.TrimStart().Length;
                        var tail = s.Length - s.TrimEnd().Length;
                        var mark = b && i ? "***" : b ? "**" : "*";
                        sb.Append(s.Substring(0, lead)).Append(mark).Append(s.Trim()).Append(mark).Append(s.Substring(s.Length - tail));
                    }
                    else sb.Append(s);
                    k++;
                }
                return sb.ToString();
            }

            private void Collect(XElement parent, List<(string, bool, bool)> runs, bool plain)
            {
                foreach (var e in parent.Elements())
                {
                    var n = e.Name;
                    if (n == W + "r") Run(e, runs);
                    else if (n == W + "hyperlink" || n == W + "ins" || n == W + "smartTag" || n == W + "fldSimple" || n == W + "customXml")
                        Collect(e, runs, plain);
                    else if (n == W + "sdt") Collect(e.Element(W + "sdtContent") ?? new XElement("x"), runs, plain);
                    else if (n == W + "del" || n == W + "moveFrom") { } // tracked deletion: as accepted
                    else if (n == M + "oMath" || n == M + "oMathPara") { if (_count) _equations++; }
                }
            }

            private void Run(XElement r, List<(string, bool, bool)> runs)
            {
                var rPr = r.Element(W + "rPr");
                bool b = On(rPr?.Element(W + "b")), i = On(rPr?.Element(W + "i"));
                foreach (var e in r.Elements())
                {
                    var n = e.Name;
                    if (n == W + "t") runs.Add(((string)e ?? "", b, i));
                    else if (n == W + "tab") runs.Add((" ", b, i));
                    else if (n == W + "br" || n == W + "cr") runs.Add(("\n", b, i));
                    else if (n == W + "noBreakHyphen") runs.Add(("-", b, i));
                    else if (n == W + "drawing" || n == W + "pict" || n == W + "object")
                    {
                        if (e.Descendants(W + "txbxContent").Any()) { if (_count) _textboxes++; }
                        else { if (_count) _images++; }
                    }
                    else if (n == W + "footnoteReference" || n == W + "endnoteReference") { if (_count) _footnotes++; }
                    else if (n == W + "commentReference") { if (_count) _comments++; }
                }
                // pictures wrapped in mc:AlternateContent
                if (r.Elements().Any(x => x.Name.LocalName == "AlternateContent"))
                {
                    var alt = r.Elements().First(x => x.Name.LocalName == "AlternateContent");
                    if (alt.Descendants(W + "txbxContent").Any()) { if (_count) _textboxes++; }
                    else if (alt.Descendants().Any(x => x.Name.LocalName == "drawing" || x.Name.LocalName == "pict")) { if (_count) _images++; }
                }
            }

            private static bool On(XElement flag)
            {
                if (flag == null) return false;
                var v = ((string)flag.Attribute(W + "val") ?? "true").ToLowerInvariant();
                return v != "0" && v != "false" && v != "none";
            }

            private void Table(XElement tbl)
            {
                EndList();
                _lastWasList = false;
                var rows = new List<List<string>>();
                foreach (var tr in tbl.Elements(W + "tr"))
                {
                    var row = new List<string>();
                    foreach (var tc in tr.Elements(W + "tc").Concat(tr.Elements(W + "sdt").SelectMany(s => s.Element(W + "sdtContent")?.Elements(W + "tc") ?? Enumerable.Empty<XElement>())))
                    {
                        var tcPr = tc.Element(W + "tcPr");
                        var vMerge = tcPr?.Element(W + "vMerge");
                        bool continued = vMerge != null && (string)vMerge.Attribute(W + "val") != "restart";
                        string text = "";
                        if (continued) _merged++;
                        else
                        {
                            if (tc.Elements(W + "tbl").Any()) _nested++;
                            text = string.Join("<br>", tc.Descendants(W + "p")
                                .Select(p => Inline(p, false).Trim()).Where(t => t.Length > 0))
                                .Replace("\n", "<br>").Replace("|", "/"); // a "|" would split the cell (DrawingStandard reads no \\|)
                        }
                        row.Add(text);
                        int span = int.TryParse((string)tcPr?.Element(W + "gridSpan")?.Attribute(W + "val"), out var sp) ? sp : 1;
                        for (int k = 1; k < span; k++) { row.Add(""); _merged++; }
                    }
                    if (row.Count > 0) rows.Add(row);
                }
                if (rows.Count == 0) return;
                int cols = rows.Max(r => r.Count);
                foreach (var r in rows) while (r.Count < cols) r.Add("");
                // "Bảng layer chuẩn" needs layer names in `…` (DrawingStandard); nobody types backticks in Word
                int layerCol = rows[0].FindIndex(h => Strip(h).Equals("Layer", StringComparison.OrdinalIgnoreCase));
                if (layerCol >= 0)
                    foreach (var r in rows.Skip(1))
                    {
                        var v = Strip(r[layerCol]);
                        if (v.Length > 0 && !v.StartsWith("`")) r[layerCol] = "`" + v + "`";
                    }
                _out.Append("| ").Append(string.Join(" | ", rows[0])).Append(" |\n");
                _out.Append("|").Append(string.Concat(Enumerable.Repeat(" --- |", cols))).Append('\n');
                foreach (var r in rows.Skip(1)) _out.Append("| ").Append(string.Join(" | ", r)).Append(" |\n");
                _out.Append('\n');
                _started = true;
            }

            public DocxResult Finish()
            {
                if (_inFrontmatter) { _r.Error = "phần đầu (frontmatter) mở bằng dòng --- nhưng chưa có dòng --- đóng lại"; return _r; }
                var md = _out.ToString().TrimEnd() + "\n";
                while (md.Contains("\n\n\n")) md = md.Replace("\n\n\n", "\n\n");
                _r.Markdown = md;
                if (md.Trim().Length == 0) _r.Error = "file Word không có chữ nào dùng được";
                else if (md.Length > MaxChars) _r.Error = $"nội dung dài {md.Length} ký tự, tối đa {MaxChars}";
                if (_images > 0) _r.Dropped.Add($"{_images} ảnh / hình vẽ (skill chỉ dùng chữ; nếu ảnh chứa quy cách, hãy viết lại thành chữ hoặc bảng)");
                if (_textboxes > 0) _r.Dropped.Add($"{_textboxes} hộp chữ (text box)");
                if (_merged > 0) _r.Dropped.Add($"{_merged} ô gộp trong bảng (đã tách ra, ô thừa để trống)");
                if (_nested > 0) _r.Dropped.Add($"{_nested} bảng lồng trong ô (chỉ giữ chữ)");
                if (_footnotes > 0) _r.Dropped.Add($"{_footnotes} chú thích cuối trang");
                if (_comments > 0) _r.Dropped.Add($"{_comments} ghi chú lề (comment)");
                if (_equations > 0) _r.Dropped.Add($"{_equations} công thức toán");
                return _r;
            }
        }
    }
}
