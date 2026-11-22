using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>One row of a skill's "Bảng layer chuẩn".</summary>
    public sealed class LayerStandard
    {
        public string Name;
        public string Content;
        /// <summary>ACI 1..255, null = not given.</summary>
        public int? Color;
        public string Linetype;
        /// <summary>mm (0.25), null = not given.</summary>
        public double? Lineweight;
    }

    /// <summary>One row of "Chữ và kích thước theo tỷ lệ in": a kind of text and its height on paper.</summary>
    public sealed class TextStandard
    {
        public string Label;
        public double PaperMm;
    }

    /// <summary>
    /// The machine-readable part of the skills of a chat (docs/ke-hoach-tu-kiem.md §9, layer B; decided: read the
    /// tables of SKILL.md, no standard.json):
    /// - a section whose heading contains "Bảng layer chuẩn": table "Layer | Nội dung | Màu | Linetype | Lineweight",
    ///   layer names in backticks;
    /// - a section whose heading contains "Chữ và kích thước theo tỷ lệ in": a "Trên giấy" column in mm, the row
    ///   "Chữ kích thước" being the dimension text;
    /// - a line "Ngưỡng khe hở: 10 mm" (verify_drawing gap).
    /// </summary>
    public sealed class DrawingStandard
    {
        public readonly Dictionary<string, LayerStandard> Layers = new Dictionary<string, LayerStandard>(StringComparer.OrdinalIgnoreCase);
        public readonly List<TextStandard> Texts = new List<TextStandard>();
        public double? DimTextMm;
        public double? GapMm;
        public readonly List<string> Sources = new List<string>();

        public bool HasLayers => Layers.Count > 0;
        public bool HasTexts => Texts.Count > 0;
        public bool IsEmpty => !HasLayers && !HasTexts && DimTextMm == null && GapMm == null;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly Regex Heading = new Regex(@"^\s{0,3}#{1,6}\s+(.*)$");
        private static readonly Regex GapLine = new Regex(@"Ngưỡng khe hở\s*:\s*([0-9]+(?:[.,][0-9]+)?)\s*mm", RegexOptions.IgnoreCase);
        private static readonly Regex Number = new Regex(@"^\s*([0-9]+(?:[.,][0-9]+)?)");

        /// <summary>Reads one SKILL.md. Format problems (shown to the user when the skill is added) go to problems.</summary>
        public static DrawingStandard Parse(string md, string source, List<string> problems = null)
        {
            problems = problems ?? new List<string>();
            var std = new DrawingStandard();
            if (!string.IsNullOrEmpty(source)) std.Sources.Add(source);
            var lines = (md ?? "").Replace("\r\n", "\n").Split('\n');

            var gm = GapLine.Match(md ?? "");
            if (gm.Success)
            {
                if (TryNum(gm.Groups[1].Value, out var g) && g > 0 && g <= 1000) std.GapMm = g;
                else problems.Add($"\"Ngưỡng khe hở\": \"{gm.Groups[1].Value}\" không phải số mm từ 0 tới 1000.");
            }

            for (int i = 0; i < lines.Length; i++)
            {
                var h = Heading.Match(lines[i]);
                if (!h.Success) continue;
                string title = h.Groups[1].Value.ToLowerInvariant();
                if (title.Contains("bảng layer chuẩn")) ReadLayers(lines, i + 1, std, problems);
                else if (title.Contains("chữ và kích thước theo tỷ lệ in")) ReadTexts(lines, i + 1, std, problems);
            }
            return std;
        }

        /// <summary>Rows of the first Markdown table of the section starting at line `from` (header first), with their line numbers.</summary>
        private static List<(int line, List<string> cells)> Table(string[] lines, int from)
        {
            var rows = new List<(int, List<string>)>();
            for (int i = from; i < lines.Length; i++)
            {
                if (Heading.IsMatch(lines[i])) break;
                var t = lines[i].Trim();
                if (!t.StartsWith("|"))
                {
                    if (rows.Count > 0) break; // the table ended
                    continue;
                }
                var cells = t.Trim('|').Split('|').Select(c => c.Trim()).ToList();
                if (cells.All(c => Regex.IsMatch(c, @"^:?-{2,}:?$"))) continue; // separator row
                rows.Add((i + 1, cells));
            }
            return rows;
        }

        private static int Col(List<string> header, string prefix) =>
            header.FindIndex(c => c.ToLowerInvariant().StartsWith(prefix.ToLowerInvariant(), StringComparison.Ordinal));

        private static void ReadLayers(string[] lines, int from, DrawingStandard std, List<string> problems)
        {
            var rows = Table(lines, from);
            if (rows.Count == 0) { problems.Add("Mục \"Bảng layer chuẩn\" không có bảng."); return; }
            var head = rows[0].cells;
            int cLayer = Col(head, "layer"), cContent = Col(head, "nội dung"), cColor = Col(head, "màu"), cLt = Col(head, "linetype"), cLw = Col(head, "lineweight");
            if (cLayer < 0) { problems.Add($"Bảng layer chuẩn (dòng {rows[0].line}): thiếu cột \"Layer\"."); return; }
            foreach (var (line, cells) in rows.Skip(1))
            {
                string Cell(int c) => c >= 0 && c < cells.Count ? cells[c] : "";
                var m = Regex.Match(Cell(cLayer), @"^`([^`]+)`$");
                if (!m.Success) { problems.Add($"Bảng layer chuẩn, dòng {line}: tên layer phải nằm trong dấu ` ` (ví dụ `A-WALL`)."); continue; }
                var row = new LayerStandard { Name = m.Groups[1].Value.Trim(), Content = Cell(cContent) };
                var color = Cell(cColor);
                if (color.Length > 0)
                {
                    var cm = Regex.Match(color, @"^\s*(\d{1,3})\b");
                    if (cm.Success && int.TryParse(cm.Groups[1].Value, NumberStyles.Integer, Inv, out var aci) && aci >= 1 && aci <= 255) row.Color = aci;
                    else problems.Add($"Bảng layer chuẩn, dòng {line}: màu \"{color}\" phải bắt đầu bằng số màu ACI 1–255.");
                }
                var lt = Cell(cLt);
                if (lt.Length > 0)
                {
                    if (Regex.IsMatch(lt, @"^[A-Za-z0-9_\-.$]+$")) row.Linetype = lt.ToUpperInvariant();
                    else problems.Add($"Bảng layer chuẩn, dòng {line}: linetype \"{lt}\" phải là một tên, ví dụ CONTINUOUS.");
                }
                var lw = Cell(cLw);
                if (lw.Length > 0)
                {
                    var nm = Number.Match(lw);
                    if (nm.Success && TryNum(nm.Groups[1].Value, out var w) && w >= 0 && w <= 2.11) row.Lineweight = w;
                    else problems.Add($"Bảng layer chuẩn, dòng {line}: lineweight \"{lw}\" phải là số mm, ví dụ 0.25.");
                }
                if (!std.Layers.ContainsKey(row.Name)) std.Layers[row.Name] = row;
            }
            if (std.Layers.Count == 0 && !problems.Any(p => p.StartsWith("Bảng layer chuẩn, dòng", StringComparison.Ordinal)))
                problems.Add("Bảng layer chuẩn không có dòng layer nào.");
        }

        private static void ReadTexts(string[] lines, int from, DrawingStandard std, List<string> problems)
        {
            var rows = Table(lines, from);
            if (rows.Count == 0) { problems.Add("Mục \"Chữ và kích thước theo tỷ lệ in\" không có bảng."); return; }
            var head = rows[0].cells;
            int cPaper = Col(head, "trên giấy");
            if (cPaper < 0) { problems.Add($"Bảng chữ (dòng {rows[0].line}): thiếu cột \"Trên giấy\"."); return; }
            foreach (var (line, cells) in rows.Skip(1))
            {
                string label = cells.Count > 0 ? cells[0] : "";
                string paper = cPaper < cells.Count ? cells[cPaper] : "";
                var nm = Number.Match(paper);
                if (!nm.Success || !TryNum(nm.Groups[1].Value, out var mm) || mm <= 0 || mm > 50)
                {
                    problems.Add($"Bảng chữ, dòng {line}: \"Trên giấy\" = \"{paper}\" phải là chiều cao mm, ví dụ 2.5 mm.");
                    continue;
                }
                if (label.ToLowerInvariant().Contains("kích thước")) std.DimTextMm = std.DimTextMm ?? mm;
                else std.Texts.Add(new TextStandard { Label = label, PaperMm = mm });
            }
        }

        private static bool TryNum(string s, out double v) =>
            double.TryParse(s.Replace(',', '.'), NumberStyles.Float, Inv, out v);

        /// <summary>The standards of several skills of one chat: first skill wins on conflicts.</summary>
        public static DrawingStandard Merge(IEnumerable<DrawingStandard> list)
        {
            var r = new DrawingStandard();
            foreach (var s in list.Where(x => x != null))
            {
                foreach (var kv in s.Layers) if (!r.Layers.ContainsKey(kv.Key)) r.Layers[kv.Key] = kv.Value;
                foreach (var t in s.Texts) if (!r.Texts.Any(x => Math.Abs(x.PaperMm - t.PaperMm) < 1e-9 && x.Label == t.Label)) r.Texts.Add(t);
                r.DimTextMm = r.DimTextMm ?? s.DimTextMm;
                r.GapMm = r.GapMm ?? s.GapMm;
                r.Sources.AddRange(s.Sources);
            }
            return r;
        }

        /// <summary>"1:100" (also "1/100", "100") → 100; null when unknown.</summary>
        public static double? ScaleDenominator(string scale)
        {
            if (string.IsNullOrWhiteSpace(scale)) return null;
            var m = Regex.Match(scale, @"^\s*(?:1\s*[:/]\s*)?([0-9]+(?:[.,][0-9]+)?)\s*$");
            if (!m.Success || !TryNum(m.Groups[1].Value, out var d) || d <= 0) return null;
            return d;
        }

        /// <summary>Allowed model-space text heights at this scale (paper height × scale).</summary>
        public List<double> TextHeights(double scale) => Texts.Select(t => t.PaperMm * scale).Distinct().OrderBy(x => x).ToList();

        public bool TextHeightOk(double height, double scale) =>
            !HasTexts || TextHeights(scale).Any(h => Math.Abs(height - h) <= h * 0.01);

        /// <summary>Default height of a text without one: the "ghi chú" row (else the smallest) × scale; null without a table.</summary>
        public double? DefaultTextHeight(double scale)
        {
            if (!HasTexts) return null;
            var note = Texts.FirstOrDefault(t => t.Label.ToLowerInvariant().Contains("ghi chú")) ?? Texts.OrderBy(t => t.PaperMm).First();
            return note.PaperMm * scale;
        }

        /// <summary>Standard layer names for an error message: those whose prefix matches, else all (at most 12).</summary>
        public string LayerList(string near = null)
        {
            var names = Layers.Keys.ToList();
            if (!string.IsNullOrEmpty(near))
            {
                var pre = near.Split('-')[0];
                var same = names.Where(n => n.StartsWith(pre + "-", StringComparison.OrdinalIgnoreCase)).ToList();
                if (same.Count > 0) names = same;
            }
            return string.Join(", ", names.Take(12)) + (names.Count > 12 ? ", ..." : "");
        }

        /// <summary>Layer names of the drawing a user message names ("vẽ lên layer XYZ", "@XYZ"): whole words, any case.</summary>
        public static bool TextNamesLayer(string text, string layer)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(layer)) return false;
            return Regex.IsMatch(text, @"(?<![\p{L}\p{N}_\-])" + Regex.Escape(layer) + @"(?![\p{L}\p{N}_\-])", RegexOptions.IgnoreCase);
        }
    }
}
