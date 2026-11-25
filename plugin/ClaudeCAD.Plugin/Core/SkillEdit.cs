using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>How "Sửa" opens a skill (v0.9.8, docs/ke-hoach-sua-skill-mo-bang.md).</summary>
    public enum SkillEditMode { App, OpenWith, Word }

    /// <summary>What a save in the editor brought back.</summary>
    public sealed class SkillEditRead
    {
        public bool Changed;          // different from what was opened (or from the last save handled)
        public string Body;           // skill text without the frontmatter
        public string SuggestedName;  // for "Tạo skill mới": the name typed in the frontmatter (.md) or <name>-sua
        public string SuggestedDescription;
        public string Warning;        // "Bảng layer chuẩn, dòng …" problems (null when fine)
        public string Dropped;        // Word: what could not come back (images, merged cells…)
        public string Error;          // the saved file cannot be read (shown, nothing written)
    }

    /// <summary>
    /// One "Sửa" in progress. A You skill opened in an app is edited in place (the library watcher reloads it). Anything
    /// else goes through a working copy in %LocalAppData%\ClaudeCAD\skill-edit: a SKILL.md copy (Default / Admin in an
    /// app) or SKILL.docx without the frontmatter (Word, any label). The original Default / Admin file is never written;
    /// a save there leads to "Tạo skill mới từ skill gốc", after which the session writes to the new You skill.
    /// </summary>
    public sealed class SkillEditSession
    {
        public string Original { get; private set; }  // the skill "Sửa" was pressed on
        public string Label { get; private set; }     // Admin / Default / You at the start
        public SkillEditMode Mode { get; private set; }
        public string WorkFile { get; private set; }  // the file the editor opens
        public bool InPlace { get; private set; }     // You skill in an app: WorkFile is the library file itself
        /// <summary>You skill the saves go to: the original when it is You, else null until "Tạo skill mới".</summary>
        public string Target { get; set; }

        private string _front, _description, _baseline;

        public static SkillEditSession Start(SkillStore store, string name, SkillEditMode mode, string workRoot)
        {
            var info = store.Scan().Find(k => k.Name == name);
            if (info == null) throw new InvalidOperationException("skill not found: " + name);
            var md = File.ReadAllText(info.File, Encoding.UTF8);
            var (front, body) = SkillStore.SplitFrontmatter(md);
            var (_, desc, _) = SkillStore.ParseFrontmatter(md);
            var s = new SkillEditSession
            {
                Original = name, Label = info.Label, Mode = mode, _front = front, _description = desc ?? "",
                Target = info.Label == "You" ? name : null,
            };
            if (info.Label == "You" && mode != SkillEditMode.Word)
            {
                s.InPlace = true;
                s.WorkFile = info.File;
                s._baseline = SampleSkills.Hash(Encoding.UTF8.GetBytes(md));
                return s;
            }
            var dir = Path.Combine(workRoot, name + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(dir);
            if (mode == SkillEditMode.Word)
            {
                s.WorkFile = Path.Combine(dir, name + ".docx");
                File.WriteAllBytes(s.WorkFile, MarkdownToDocx.Convert(body));
                // what this very file reads back as: a save without a change is not a change
                using (var fs = File.OpenRead(s.WorkFile)) s._baseline = Norm(DocxToMarkdown.Convert(fs).Markdown);
            }
            else
            {
                s.WorkFile = Path.Combine(dir, "SKILL.md");
                File.WriteAllText(s.WorkFile, md, new UTF8Encoding(false));
                s._baseline = SampleSkills.Hash(File.ReadAllBytes(s.WorkFile));
            }
            return s;
        }

        private static string Norm(string body) => (body ?? "").Replace("\r\n", "\n").Trim();

        /// <summary>Reads the working file after the editor saved it. Changed = false when there is nothing new.</summary>
        public SkillEditRead Read()
        {
            var r = new SkillEditRead();
            if (Mode == SkillEditMode.Word)
            {
                var c = DocxToMarkdown.Convert(WorkFile);
                if (c.Error != null) { r.Error = c.Error; return r; }
                var body = c.HasFrontmatter ? SkillStore.SplitFrontmatter(c.Markdown).body : c.Markdown;
                r.Changed = Norm(body) != _baseline;
                r.Body = body;
                r.Dropped = c.Dropped.Count > 0 ? string.Join("; ", c.Dropped) : null;
                r.SuggestedName = Original + SkillStore.CopySuffix;
                r.SuggestedDescription = _description;
            }
            else
            {
                byte[] bytes;
                try { bytes = File.ReadAllBytes(WorkFile); }
                catch (IOException ex) { r.Error = "chưa đọc được file (đang được ghi?): " + ex.Message; return r; }
                r.Changed = SampleSkills.Hash(bytes) != _baseline;
                var md = Encoding.UTF8.GetString(bytes);
                var (front, body) = SkillStore.SplitFrontmatter(md);
                var (name, desc, _) = SkillStore.ParseFrontmatter(md);
                r.Body = body;
                // a name typed in the copy's frontmatter is the user's choice for the new skill
                r.SuggestedName = !string.IsNullOrWhiteSpace(name) && name.Trim() != Original ? name.Trim() : Original + SkillStore.CopySuffix;
                r.SuggestedDescription = string.IsNullOrWhiteSpace(desc) ? _description : desc;
                if (front.Length == 0) r.Warning = "file đã mất phần đầu (name / description); plugin dùng lại phần đầu cũ";
            }
            var preview = SkillStore.WithFrontmatter(Target ?? r.SuggestedName, r.SuggestedDescription.Length > 0 ? r.SuggestedDescription : "x", r.Body);
            var table = SkillStore.StandardWarning(preview);
            if (table != null) r.Warning = r.Warning == null ? table : r.Warning + ". " + table;
            return r;
        }

        /// <summary>The save was handled (written, or turned into a new skill): the next save is compared to this one.</summary>
        public void Accept(SkillEditRead read)
        {
            if (Mode == SkillEditMode.Word) _baseline = Norm(read.Body);
            else _baseline = SampleSkills.Hash(File.ReadAllBytes(WorkFile));
        }

        /// <summary>Removes working folders older than <paramref name="days"/> days (plugin start).</summary>
        public static int CleanWorkRoot(string workRoot, int days = 7)
        {
            int n = 0;
            if (!Directory.Exists(workRoot)) return 0;
            foreach (var d in Directory.GetDirectories(workRoot))
            {
                try
                {
                    if (Directory.GetLastWriteTime(d) < DateTime.Now.AddDays(-days)) { Directory.Delete(d, true); n++; }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return n;
        }
    }
}
