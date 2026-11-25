using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>One skill in the plugin's skill folder, merged with its registry entry.</summary>
    public sealed class SkillInfo
    {
        public string Name;
        public string Title;            // first "# heading" of SKILL.md, else the name
        public string Description;
        public bool Enabled = true;
        public bool Builtin;            // shipped sample skill
        public DateTime? Added;
        public string Source;           // file the user imported it from
        public string Folder;
        public string File;
        public string Error;            // invalid SKILL.md (shown in the list, not loaded)
        /// <summary>The skill works, but its standard tables cannot be read by the plugin (layer B), first problems.</summary>
        public string Warning;
        public bool Admin;              // pushed by the owner (Google Sheet "Gán skill"), not edited since
        public string AdminName;        // its name on the sheet (the folder is "<name>-admin" when the name was taken)
        public bool Hidden;             // the owner chose "Ẩn": kept on disk, not listed, never loaded
        public string EditOf;           // a You copy made from this Default / Admin skill (button Sửa, or an edit outside the plugin)
        /// <summary>Label next to the name: Admin (pushed by the owner), Default (shipped sample), You (the user's own).</summary>
        public string Label => Admin ? "Admin" : Builtin ? "Default" : "You";
        /// <summary>Name to show: the sheet name for an owner skill stored as "&lt;name&gt;-admin".</summary>
        public string DisplayName => Admin && !string.IsNullOrEmpty(AdminName) ? AdminName : Name;
    }

    public enum PackChangeKind { Added, Updated, Hidden, Shown, Deleted, Skipped }

    /// <summary>What ApplyPack did to one skill (for the toast / note).</summary>
    public sealed class PackChange
    {
        public PackChangeKind Kind;
        public string Name;    // sheet name
        public string Folder;  // library name (may end in -admin)
        public string Note;
    }

    public enum SkillImportStatus { Added, Updated, AlreadyAdded, Conflict, Invalid }

    /// <summary>
    /// Skills per chat (v0.9.4): the library is every skill ever added (the plugin's skill folder); each chat
    /// uses a subset ("−" / "+"). A new chat starts with the skills of the most recent chat.
    /// </summary>
    public static class SkillSelection
    {
        /// <summary>Valid, distinct names, order kept.</summary>
        public static List<string> Clean(IEnumerable<string> names) =>
            (names ?? Enumerable.Empty<string>()).Where(SkillStore.IsValidName).Distinct(StringComparer.Ordinal).ToList();

        /// <summary>
        /// Skills a chat uses: its own choice, else the most recent chat's, else (first run after updating from a
        /// version without per-chat skills) the skills the user had added and enabled — never the shipped samples.
        /// Only skills that still exist and are readable are kept.
        /// </summary>
        public static List<string> ForChat(List<string> chat, List<string> last, IEnumerable<SkillInfo> library)
        {
            var names = new HashSet<string>((library ?? Enumerable.Empty<SkillInfo>()).Where(k => k.Error == null && !k.Hidden).Select(k => k.Name), StringComparer.Ordinal);
            return Choice(chat, last, library).Where(names.Contains).ToList();
        }

        /// <summary>The choice itself (chat ?? most recent ?? enabled own skills), not filtered by what exists now.</summary>
        public static List<string> Choice(List<string> chat, List<string> last, IEnumerable<SkillInfo> library) =>
            Clean(chat ?? last ?? (library ?? Enumerable.Empty<SkillInfo>()).Where(k => k.Error == null && k.Enabled && !k.Builtin && !k.Admin && !k.Hidden).Select(k => k.Name).ToList());

        /// <summary>"+" (use = true) / "−" (use = false) on one skill.</summary>
        public static List<string> Toggle(List<string> current, string name, bool use)
        {
            var list = Clean(current ?? new List<string>());
            list.Remove(name);
            if (use && SkillStore.IsValidName(name)) list.Add(name);
            return list;
        }
    }

    public sealed class SkillImportResult
    {
        public SkillImportStatus Status;
        public string Name;
        public string Message;
        /// <summary>Format problems of the standard tables (null = none).</summary>
        public string Warning;
    }

    /// <summary>
    /// The skills added to the plugin through "+ Add skill": each skill is a folder
    /// &lt;root&gt;\&lt;name&gt;\SKILL.md, and &lt;root&gt;\skills.json records which skills were
    /// added, when, from which file, and whether they are enabled. The sidecar reads the
    /// same registry and loads only enabled skills.
    /// </summary>
    public sealed class SkillStore
    {
        public const string RegistryFileName = "skills.json";
        public const int MaxDescription = 1024;
        public static readonly string[] BuiltinNames = { "ve-kien-truc", "ve-mep", "ve-ha-tang" };
        private static readonly Regex NameRx = new Regex("^[a-z0-9][a-z0-9-]{0,63}$");
        private static readonly string[] CopyExtensions = { ".md", ".txt", ".csv", ".json" };

        public string Root { get; }
        // the palette (UI thread) and a pushed pack (status check, worker thread) share one store: a Scan between
        // writing an owner's SKILL.md and saving skills.json would take our own write for a user edit
        private readonly object _gate = new object();
        public string RegistryPath => Path.Combine(Root, RegistryFileName);

        public SkillStore(string root)
        {
            Root = root;
        }

        // ------------------------------------------------------------------ parsing

        /// <summary>
        /// Reads the YAML frontmatter (name, description). Returns the error to show the
        /// user when the file cannot be a skill.
        /// </summary>
        public static (string name, string description, string error) ParseFrontmatter(string md)
        {
            if (string.IsNullOrWhiteSpace(md)) return (null, null, "File trống.");
            var text = md.TrimStart('﻿').Replace("\r\n", "\n");
            if (!text.StartsWith("---\n")) return (null, null, "File chưa có phần đầu (frontmatter) với name và description.");
            int end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
            if (end < 0) return (null, null, "Phần đầu (frontmatter) chưa được đóng bằng dòng ---.");
            string name = null, desc = null;
            foreach (var raw in text.Substring(4, end - 4).Split('\n'))
            {
                var line = raw.TrimEnd();
                int colon = line.IndexOf(':');
                if (colon <= 0 || char.IsWhiteSpace(line[0])) continue;
                var key = line.Substring(0, colon).Trim().ToLowerInvariant();
                var value = line.Substring(colon + 1).Trim().Trim('"', '\'');
                if (key == "name") name = value;
                else if (key == "description") desc = value;
            }
            if (string.IsNullOrWhiteSpace(desc))
                return (name, null, "Thiếu dòng description (mô tả khi nào dùng skill) — Claude dựa vào dòng này để biết lúc nào nạp skill.");
            if (desc.Length < 20) return (name, desc, "Dòng description quá ngắn: hãy viết rõ khi nào dùng skill và các từ khóa (ít nhất 20 ký tự).");
            if (desc.Length > MaxDescription) return (name, desc, $"Dòng description dài {desc.Length} ký tự, tối đa {MaxDescription}.");
            return (name, desc, null);
        }

        /// <summary>"Vẽ Kết Cấu (v2).md" → "ve-ket-cau-v2".</summary>
        public static string Slugify(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var normalized = s.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c) && c < 128) sb.Append(char.ToLowerInvariant(c));
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            }
            var slug = sb.ToString().Trim('-');
            return slug.Length > 64 ? slug.Substring(0, 64).TrimEnd('-') : slug;
        }

        public static bool IsValidName(string n) => !string.IsNullOrEmpty(n) && NameRx.IsMatch(n);

        /// <summary>Text on one line (a description typed in a dialog goes into the one-line frontmatter).</summary>
        public static string OneLine(string s) => Regex.Replace(s ?? "", @"\s+", " ").Trim();

        /// <summary>Why a name / description typed for a new skill (Word file without frontmatter) cannot be used, or null.</summary>
        public static string CheckNewSkillInfo(string name, string description)
        {
            var slug = Slugify(name);
            if (!IsValidName(slug)) return "Tên skill trống hoặc không hợp lệ: chỉ dùng chữ thường không dấu, số và gạch nối, ví dụ ve-ket-cau.";
            var d = OneLine(description);
            if (d.Length < 20) return "Mô tả quá ngắn: viết rõ khi nào dùng skill và các từ khóa (ít nhất 20 ký tự).";
            if (d.Length > MaxDescription) return $"Mô tả dài {d.Length} ký tự, tối đa {MaxDescription}.";
            return null;
        }

        /// <summary>"---\nname: …\ndescription: …\n---\n\n" + body (a Word file converted without its own frontmatter).</summary>
        public static string WithFrontmatter(string name, string description, string body) =>
            "---\nname: " + Slugify(name) + "\ndescription: " + OneLine(description) + "\n---\n\n" + (body ?? "").TrimStart();

        private static string TitleOf(string md, string fallback)
        {
            foreach (var line in md.Replace("\r\n", "\n").Split('\n'))
                if (line.StartsWith("# ")) return line.Substring(2).Trim();
            return fallback;
        }

        // ------------------------------------------------------------------ registry

        private sealed class Entry
        {
            public string Name;
            public bool Enabled = true;
            public DateTime? Added;
            public string Source;
            public string Origin;   // "admin" = pushed by the owner; null = the user's own (or a shipped sample)
            public string AdminName;
            public string Rev;
            public string PackSha;  // sha256 of the skill as the owner sent it
            public string FileSha;  // sha256 of the SKILL.md written: a different file means the user edited it
            public bool Hidden;
            public string EditOf;   // the Default / Admin skill this You copy was made from
            public string EditSha;  // SampleSkills.Hash of the copy as made: same content = never edited (yet)
        }

        private Dictionary<string, Entry> LoadRegistry()
        {
            var map = new Dictionary<string, Entry>(StringComparer.Ordinal);
            try
            {
                if (!System.IO.File.Exists(RegistryPath)) return map;
                var d = JsonCodec.ParseObject(System.IO.File.ReadAllText(RegistryPath, Encoding.UTF8));
                if (!(d.TryGetValue("skills", out var sv) && sv is List<object> list)) return map;
                foreach (var o in list.OfType<Dictionary<string, object>>())
                {
                    var e = new Entry { Name = o.TryGetValue("name", out var n) ? n as string : null };
                    if (string.IsNullOrEmpty(e.Name)) continue;
                    if (o.TryGetValue("enabled", out var en) && en is bool b) e.Enabled = b;
                    if (o.TryGetValue("added", out var ad) && ad is string a && DateTime.TryParse(a, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t)) e.Added = t;
                    if (o.TryGetValue("source", out var src)) e.Source = src as string;
                    if (o.TryGetValue("origin", out var og)) e.Origin = og as string;
                    if (o.TryGetValue("adminName", out var an)) e.AdminName = an as string;
                    if (o.TryGetValue("rev", out var rv)) e.Rev = rv as string;
                    if (o.TryGetValue("packSha", out var ps)) e.PackSha = ps as string;
                    if (o.TryGetValue("fileSha", out var fs)) e.FileSha = fs as string;
                    if (o.TryGetValue("hidden", out var hd) && hd is bool h) e.Hidden = h;
                    if (o.TryGetValue("editOf", out var eo)) e.EditOf = eo as string;
                    if (o.TryGetValue("editSha", out var es)) e.EditSha = es as string;
                    map[e.Name] = e;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("skills.json unreadable, rebuilding: " + ex.Message);
            }
            return map;
        }

        private void SaveRegistry(IEnumerable<Entry> entries)
        {
            Directory.CreateDirectory(Root);
            var d = new Dictionary<string, object>
            {
                ["version"] = 1,
                ["note"] = "Danh sách skill của ClaudeCAD (quản lý bằng nút Skill / + Add skill trên khung chat).",
                ["skills"] = entries.OrderBy(e => e.Name, StringComparer.Ordinal).Select(e =>
                {
                    var o = new Dictionary<string, object>
                    {
                        ["name"] = e.Name,
                        ["enabled"] = e.Enabled,
                        ["added"] = e.Added?.ToString("o", CultureInfo.InvariantCulture),
                        ["source"] = e.Source,
                    };
                    if (e.Origin != null)
                    {
                        o["origin"] = e.Origin;
                        o["adminName"] = e.AdminName;
                        o["rev"] = e.Rev;
                        o["packSha"] = e.PackSha;
                        o["fileSha"] = e.FileSha;
                    }
                    if (e.Hidden) o["hidden"] = true; // read by the sidecar too (agent/src/agent/skills.ts)
                    if (e.EditOf != null) o["editOf"] = e.EditOf;
                    if (e.EditSha != null) o["editSha"] = e.EditSha;
                    return (object)o;
                }).ToList(),
            };
            var tmp = RegistryPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            System.IO.File.WriteAllText(tmp, JsonCodec.Serialize(d), new UTF8Encoding(false));
            if (System.IO.File.Exists(RegistryPath)) System.IO.File.Replace(tmp, RegistryPath, null, true);
            else System.IO.File.Move(tmp, RegistryPath);
        }

        // ------------------------------------------------------------------ queries

        /// <summary>All skill folders (with SKILL.md) merged with the registry; new folders are registered as enabled.</summary>
        public List<SkillInfo> Scan() { lock (_gate) return ScanCore(); }

        private List<SkillInfo> ScanCore()
        {
            var result = new List<SkillInfo>();
            if (string.IsNullOrEmpty(Root) || !Directory.Exists(Root)) return result;
            var reg = LoadRegistry();
            bool changed = SplitOutsideEdits(reg);
            foreach (var dir in Directory.GetDirectories(Root).OrderBy(d => d, StringComparer.Ordinal))
            {
                var folder = Path.GetFileName(dir);
                if (folder.StartsWith(".") || folder.StartsWith("_")) continue;
                var file = Path.Combine(dir, "SKILL.md");
                if (!System.IO.File.Exists(file)) continue;
                var info = new SkillInfo { Name = folder, Folder = dir, File = file, Builtin = BuiltinNames.Contains(folder) };
                try
                {
                    var md = System.IO.File.ReadAllText(file, Encoding.UTF8);
                    var (name, desc, error) = ParseFrontmatter(md);
                    info.Description = desc;
                    info.Title = TitleOf(md, folder);
                    if (error != null) info.Error = error;
                    else if (name != folder) info.Error = $"name trong file là \"{name}\" nhưng thư mục là \"{folder}\"; hai tên phải giống nhau.";
                    else info.Warning = StandardWarning(md);
                }
                catch (Exception ex)
                {
                    info.Error = "Không đọc được SKILL.md: " + ex.Message;
                }
                if (!reg.TryGetValue(folder, out var e))
                {
                    e = new Entry { Name = folder, Enabled = true, Added = info.Builtin ? (DateTime?)null : System.IO.File.GetCreationTime(file) };
                    reg[folder] = e;
                    changed = true;
                }
                if (e.Origin == AdminOrigin && e.FileSha != null && FileSha(file) != e.FileSha)
                {
                    // edited outside the plugin and no saved original to split from (SplitOutsideEdits): it is theirs now
                    Logger.Info($"skill {folder}: edited by the user, no saved original, now a You skill");
                    e.Origin = null; e.AdminName = null; e.Rev = null; e.PackSha = null; e.FileSha = null;
                    changed = true;
                }
                info.Enabled = e.Enabled;
                info.Added = e.Added;
                info.Source = e.Source;
                info.Admin = e.Origin == AdminOrigin;
                info.AdminName = info.Admin ? e.AdminName : null;
                info.Hidden = info.Admin && e.Hidden;
                info.EditOf = e.EditOf;
                result.Add(info);
            }
            // forget registry entries whose folder is gone
            foreach (var gone in reg.Keys.Where(k => !result.Any(r => r.Name == k)).ToList()) { reg.Remove(gone); changed = true; }
            if (changed)
            {
                try { SaveRegistry(reg.Values); }
                catch (Exception ex) { Logger.Warn("skills.json not saved: " + ex.Message); }
            }
            return result;
        }

        public bool IsWritable()
        {
            try
            {
                Directory.CreateDirectory(Root);
                var probe = Path.Combine(Root, ".write-test-" + Guid.NewGuid().ToString("N"));
                System.IO.File.WriteAllText(probe, "x");
                System.IO.File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ------------------------------------------------------------------ changes

        /// <summary>
        /// Adds a skill from a .md file chosen by the user. A file named SKILL.md brings its
        /// whole folder (reference files); any other .md becomes &lt;name&gt;\SKILL.md.
        /// overwrite=false returns Conflict when a different skill of that name exists.
        /// </summary>
        public SkillImportResult Import(string mdPath, bool overwrite) { lock (_gate) return ImportCore(mdPath, overwrite); }

        private SkillImportResult ImportCore(string mdPath, bool overwrite)
        {
            string md;
            try { md = System.IO.File.ReadAllText(mdPath, Encoding.UTF8); }
            catch (Exception ex) { return Invalid(Path.GetFileName(mdPath), "Không đọc được file: " + ex.Message); }
            bool isSkillFile = Path.GetFileName(mdPath).Equals("SKILL.md", StringComparison.OrdinalIgnoreCase);
            string fallback = isSkillFile ? Path.GetFileName(Path.GetDirectoryName(mdPath)) : Path.GetFileNameWithoutExtension(mdPath);
            return ImportMarkdown(md, Path.GetFileName(mdPath), fallback, mdPath, isSkillFile, overwrite);
        }

        /// <summary>
        /// A skill from text that is not a file in the library (v0.9.8: a Word file converted by DocxToMarkdown):
        /// the same checks as Import (frontmatter, name, conflict, standard tables). <paramref name="source"/> is
        /// the original file, recorded in skills.json; nothing else is copied.
        /// </summary>
        public SkillImportResult ImportText(string md, string fallbackName, string source, bool overwrite)
        {
            lock (_gate) return ImportMarkdown(md, Path.GetFileName(source ?? fallbackName ?? "skill"), fallbackName, source, false, overwrite);
        }

        private SkillImportResult ImportMarkdown(string md, string display, string fallback, string mdPath, bool isSkillFile, bool overwrite)
        {
            var (fmName, desc, error) = ParseFrontmatter(md);
            if (error != null) return Invalid(display, error);

            string name = !string.IsNullOrWhiteSpace(fmName) ? fmName.Trim() : Slugify(fallback);
            if (!IsValidName(name))
            {
                var slug = Slugify(name);
                if (!IsValidName(slug)) return Invalid(display, $"Tên skill \"{name}\" không hợp lệ: chỉ dùng chữ thường không dấu, số và gạch nối, ví dụ ve-ket-cau.");
                name = slug;
            }

            var destDir = Path.Combine(Root, name);
            var destFile = Path.Combine(destDir, "SKILL.md");
            bool exists = System.IO.File.Exists(destFile);
            if (exists && isSkillFile && SamePath(destFile, mdPath))
            {
                SetEnabled(name, true);
                return new SkillImportResult { Status = SkillImportStatus.AlreadyAdded, Name = name, Message = $"Skill \"{name}\" đã có trong plugin (đã bật)." };
            }
            if (exists && !overwrite)
                return new SkillImportResult { Status = SkillImportStatus.Conflict, Name = name, Message = $"Plugin đã có skill \"{name}\"." };

            Directory.CreateDirectory(destDir);
            // frontmatter name must match the folder (Agent Skills format)
            var content = EnsureName(md, name);
            if (isSkillFile)
            {
                var srcDir = Path.GetDirectoryName(Path.GetFullPath(mdPath));
                foreach (var f in Directory.GetFiles(srcDir))
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (!CopyExtensions.Contains(ext) || Path.GetFileName(f).Equals("SKILL.md", StringComparison.OrdinalIgnoreCase)) continue;
                    if (new FileInfo(f).Length > 2_000_000) continue;
                    System.IO.File.Copy(f, Path.Combine(destDir, Path.GetFileName(f)), true);
                }
            }
            System.IO.File.WriteAllText(destFile, content, new UTF8Encoding(false));

            var reg = LoadRegistry();
            reg[name] = new Entry { Name = name, Enabled = true, Added = DateTime.Now, Source = string.IsNullOrEmpty(mdPath) ? null : Path.GetFullPath(mdPath) };
            SaveRegistry(reg.Values);
            return new SkillImportResult
            {
                Status = exists ? SkillImportStatus.Updated : SkillImportStatus.Added,
                Name = name,
                Message = exists ? $"Đã cập nhật skill \"{name}\"." : $"Đã thêm skill \"{name}\".",
                Warning = StandardWarning(content),
            };
        }

        /// <summary>
        /// "Bảng layer chuẩn" / "Chữ và kích thước theo tỷ lệ in" / "Ngưỡng khe hở" that the plugin cannot read
        /// (they are then not enforced): the first 3 problems, null when fine or when the skill has no such tables.
        /// </summary>
        public static string StandardWarning(string md)
        {
            var problems = new List<string>();
            DrawingStandard.Parse(md, null, problems);
            if (problems.Count == 0) return null;
            return string.Join(" ", problems.Take(3)) + (problems.Count > 3 ? $" (và {problems.Count - 3} lỗi nữa)" : "");
        }

        private static SkillImportResult Invalid(string file, string why) =>
            new SkillImportResult { Status = SkillImportStatus.Invalid, Name = file, Message = $"Không thêm được \"{file}\": {why}" };

        private static bool SamePath(string a, string b)
        {
            try { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        /// <summary>Sets "name: x" in the frontmatter (adds it when missing).</summary>
        public static string EnsureName(string md, string name)
        {
            var text = md.TrimStart('﻿').Replace("\r\n", "\n");
            int end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
            var head = text.Substring(4, end - 4);
            var lines = head.Split('\n').ToList();
            int idx = lines.FindIndex(l => l.TrimStart().StartsWith("name:", StringComparison.OrdinalIgnoreCase) && !char.IsWhiteSpace(l.Length > 0 ? l[0] : 'x'));
            if (idx >= 0) lines[idx] = "name: " + name;
            else lines.Insert(0, "name: " + name);
            return "---\n" + string.Join("\n", lines) + text.Substring(end);
        }

        public void SetEnabled(string name, bool enabled) { lock (_gate) SetEnabledCore(name, enabled); }

        private void SetEnabledCore(string name, bool enabled)
        {
            var reg = LoadRegistry();
            if (!reg.TryGetValue(name, out var e)) e = reg[name] = new Entry { Name = name, Added = DateTime.Now };
            e.Enabled = enabled;
            SaveRegistry(reg.Values);
        }

        public void Delete(string name) { lock (_gate) DeleteCore(name); }

        private void DeleteCore(string name)
        {
            if (!IsValidName(name)) throw new ArgumentException("invalid skill name");
            var dir = Path.Combine(Root, name);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            var reg = LoadRegistry();
            if (reg.Remove(name)) SaveRegistry(reg.Values);
        }

        public string SkillFile(string name) => Path.Combine(Root, name, "SKILL.md");

        // ------------------------------------------------------------------ skills pushed by the owner (v0.9.6)

        public const string AdminOrigin = "admin";

        private static string FileSha(string file)
        {
            try { return SkillPackLogic.Sha256(System.IO.File.ReadAllBytes(file)); }
            catch { return null; }
        }

        /// <summary>
        /// Applies a verified pack (docs/ke-hoach-day-skill.md §4.3). Only Admin skills are touched: a You or
        /// Default skill of the same name is kept and the owner's copy goes to "&lt;name&gt;-admin". Skills missing
        /// from the pack stay as they are. New skills are not put into any chat (the user presses +).
        /// </summary>
        public List<PackChange> ApplyPack(IEnumerable<PackSkill> skills) { lock (_gate) return ApplyPackCore(skills); }

        /// <summary>
        /// Newer shipped samples replace the installed ones the user never edited (<see cref="SampleSkills"/>),
        /// under the same lock as imports and owner packs so the two never write a skill folder at once.
        /// </summary>
        public SampleSkills.Result RefreshSamples(string pristineDir) { lock (_gate) return SampleSkills.Refresh(pristineDir, Root); }

        private List<PackChange> ApplyPackCore(IEnumerable<PackSkill> skills)
        {
            var changes = new List<PackChange>();
            Directory.CreateDirectory(Root);
            Scan(); // registers new folders and turns edited Admin skills into You skills first
            var reg = LoadRegistry();
            foreach (var s in skills ?? Enumerable.Empty<PackSkill>())
            {
                // skills.json can be edited by hand: only a valid folder name is ever written or deleted (review D)
                var mine = reg.Values.FirstOrDefault(e => e.Origin == AdminOrigin && e.AdminName == s.Name
                    && IsValidName(e.Name) && System.IO.File.Exists(SkillFile(e.Name)));
                try
                {
                    if (s.Act == PackSkill.Delete)
                    {
                        if (mine == null) continue;
                        var dir = Path.Combine(Root, mine.Name);
                        if (Directory.Exists(dir)) Directory.Delete(dir, true);
                        try { System.IO.File.Delete(OriginalFile(mine.Name)); } catch { }
                        reg.Remove(mine.Name);
                        changes.Add(new PackChange { Kind = PackChangeKind.Deleted, Name = s.Name, Folder = mine.Name });
                        continue;
                    }
                    bool hide = s.Act == PackSkill.Hide;
                    if (mine != null)
                    {
                        if (mine.PackSha != s.Sha256 || mine.Rev != s.Rev)
                        {
                            mine.FileSha = WriteSkill(mine.Name, s.Md);
                            mine.PackSha = s.Sha256;
                            mine.Rev = s.Rev;
                            if (!hide) changes.Add(new PackChange { Kind = PackChangeKind.Updated, Name = s.Name, Folder = mine.Name });
                        }
                        if (mine.Hidden != hide)
                        {
                            mine.Hidden = hide;
                            changes.Add(new PackChange { Kind = hide ? PackChangeKind.Hidden : PackChangeKind.Shown, Name = s.Name, Folder = mine.Name });
                        }
                        continue;
                    }
                    var folder = s.Name;
                    if (Directory.Exists(Path.Combine(Root, folder)) || reg.ContainsKey(folder))
                    {
                        folder = s.Name + SkillPackLogic.AdminSuffix; // keep both: the user's skill and the owner's
                        if (Directory.Exists(Path.Combine(Root, folder)) || reg.ContainsKey(folder))
                        {
                            changes.Add(new PackChange { Kind = PackChangeKind.Skipped, Name = s.Name, Folder = folder, Note = $"tên \"{s.Name}\" và \"{folder}\" đều đã có skill của người dùng" });
                            continue;
                        }
                    }
                    var sha = WriteSkill(folder, s.Md);
                    reg[folder] = new Entry
                    {
                        Name = folder, Enabled = true, Added = DateTime.Now, Source = "Chủ plugin (Admin)",
                        Origin = AdminOrigin, AdminName = s.Name, Rev = s.Rev, PackSha = s.Sha256, FileSha = sha, Hidden = hide,
                    };
                    if (!hide) changes.Add(new PackChange { Kind = PackChangeKind.Added, Name = s.Name, Folder = folder });
                }
                catch (Exception ex)
                {
                    Logger.Warn($"pushed skill {s.Name}: {ex.Message}");
                    changes.Add(new PackChange { Kind = PackChangeKind.Skipped, Name = s.Name, Folder = mine?.Name ?? s.Name, Note = ex.Message });
                }
            }
            SaveRegistry(reg.Values);
            return changes;
        }

        /// <summary>Writes &lt;folder&gt;\SKILL.md through a temp folder (no half-written skill). Returns the file's sha256.</summary>
        private string WriteSkill(string folder, string md)
        {
            var content = EnsureName(md, folder);
            var bytes = new UTF8Encoding(false).GetBytes(content);
            var dir = Path.Combine(Root, folder);
            var tmp = Path.Combine(Root, "_tmp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            System.IO.File.WriteAllBytes(Path.Combine(tmp, "SKILL.md"), bytes);
            if (Directory.Exists(dir))
            {
                var file = Path.Combine(dir, "SKILL.md");
                if (System.IO.File.Exists(file)) System.IO.File.Replace(Path.Combine(tmp, "SKILL.md"), file, null, true);
                else System.IO.File.Move(Path.Combine(tmp, "SKILL.md"), file);
                Directory.Delete(tmp, true);
            }
            else Directory.Move(tmp, dir);
            // the owner's original, to put back when the user edits the file outside the plugin
            Directory.CreateDirectory(Path.Combine(Root, OriginalsFolder));
            System.IO.File.WriteAllBytes(OriginalFile(folder), bytes);
            return SkillPackLogic.Sha256(bytes);
        }

        // ------------------------------------------------------------------ editing a Default / Admin skill (v0.9.6)

        /// <summary>Hidden folder (never a skill) with the owner's originals: "&lt;folder&gt;.md".</summary>
        public const string OriginalsFolder = ".claudecad-admin";
        public const string CopySuffix = "-sua";

        private string OriginalFile(string folder) => Path.Combine(Root, OriginalsFolder, folder + ".md");

        /// <summary>Contents\skills-mau (pristine samples) — where an edited Default skill is restored from.</summary>
        public string PristineDir { get; set; }

        private readonly List<(string copy, string original)> _pendingSwitches = new List<(string, string)>();

        private static string TextHash(string file)
        {
            try { return SampleSkills.Hash(System.IO.File.ReadAllBytes(file)); }
            catch { return null; }
        }

        /// <summary>
        /// "Sửa": a You skill opens as it is; a Default or Admin skill is first copied to &lt;name&gt;-sua (label You,
        /// frontmatter name changed) and the copy opens — the original is never written. An existing copy is reused.
        /// Returns the skill to open and whether a copy was made now.
        /// </summary>
        public (string open, bool copied) PrepareEdit(string name) { lock (_gate) return PrepareEditCore(name); }

        private (string open, bool copied) PrepareEditCore(string name)
        {
            if (!IsValidName(name) || !System.IO.File.Exists(SkillFile(name))) return (name, false);
            var reg = LoadRegistry();
            reg.TryGetValue(name, out var e);
            bool admin = e != null && e.Origin == AdminOrigin;
            if (!admin && !BuiltinNames.Contains(name)) return (name, false);
            var baseName = admin && IsValidName(e.AdminName) ? e.AdminName : name;
            var copy = baseName + CopySuffix;
            if (System.IO.File.Exists(SkillFile(copy))) return (copy, false);
            if (!IsValidName(copy) || Directory.Exists(Path.Combine(Root, copy))) return (name, false);
            var hash = CopySkill(name, copy, System.IO.File.ReadAllText(SkillFile(name), Encoding.UTF8));
            reg[copy] = new Entry { Name = copy, Enabled = true, Added = DateTime.Now, Source = "Bản sửa của " + baseName, EditOf = name, EditSha = hash };
            SaveRegistry(reg.Values);
            return (copy, true);
        }

        /// <summary>Copies the folder of <paramref name="from"/> to <paramref name="to"/> with the given SKILL.md text; returns its TextHash.</summary>
        private string CopySkill(string from, string to, string md)
        {
            var src = Path.Combine(Root, from);
            var tmp = Path.Combine(Root, "_tmp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            foreach (var f in Directory.GetFiles(src))
                if (!Path.GetFileName(f).Equals("SKILL.md", StringComparison.OrdinalIgnoreCase))
                    System.IO.File.Copy(f, Path.Combine(tmp, Path.GetFileName(f)));
            var bytes = new UTF8Encoding(false).GetBytes(EnsureName(md, to));
            System.IO.File.WriteAllBytes(Path.Combine(tmp, "SKILL.md"), bytes);
            Directory.Move(tmp, Path.Combine(Root, to));
            return SampleSkills.Hash(bytes);
        }

        /// <summary>First free "&lt;base&gt;-sua", "&lt;base&gt;-sua-2" … (an edit outside the plugin never overwrites an earlier copy).</summary>
        private string FreeCopyName(string baseName, Dictionary<string, Entry> reg)
        {
            for (int i = 1; i <= 9; i++)
            {
                var n = baseName + CopySuffix + (i == 1 ? "" : "-" + i);
                if (IsValidName(n) && !Directory.Exists(Path.Combine(Root, n)) && !reg.ContainsKey(n)) return n;
            }
            return null;
        }

        /// <summary>
        /// A Default or Admin SKILL.md edited outside the plugin (Explorer, Notepad): the edit moves to a You copy
        /// and the original is put back (Default from Contents\skills-mau, Admin from the copy saved with the pack).
        /// Returns whether the registry changed.
        /// </summary>
        private bool SplitOutsideEdits(Dictionary<string, Entry> reg)
        {
            bool changed = false;
            var todo = new List<(string folder, string baseName, byte[] original, Entry admin)>();
            if (!string.IsNullOrEmpty(PristineDir))
                foreach (var n in BuiltinNames)
                    if (System.IO.File.Exists(SkillFile(n)) && SampleSkills.IsEdited(PristineDir, Root, n + "/SKILL.md"))
                        todo.Add((n, n, System.IO.File.ReadAllBytes(Path.Combine(PristineDir, n, "SKILL.md")), null));
            foreach (var e in reg.Values.Where(x => x.Origin == AdminOrigin && x.FileSha != null && IsValidName(x.Name)).ToList())
            {
                var file = SkillFile(e.Name);
                if (!System.IO.File.Exists(file) || FileSha(file) == e.FileSha) continue;
                var orig = OriginalFile(e.Name);
                if (!System.IO.File.Exists(orig) || SkillPackLogic.Sha256(System.IO.File.ReadAllBytes(orig)) != e.FileSha) continue; // ScanCore: becomes You
                todo.Add((e.Name, IsValidName(e.AdminName) ? e.AdminName : e.Name, System.IO.File.ReadAllBytes(orig), e));
            }
            foreach (var t in todo)
            {
                try
                {
                    var copy = FreeCopyName(t.baseName, reg);
                    if (copy == null) { Logger.Warn($"skill {t.folder}: edited outside the plugin, no free name for a copy"); continue; }
                    var edited = System.IO.File.ReadAllText(SkillFile(t.folder), Encoding.UTF8);
                    CopySkill(t.folder, copy, edited);
                    System.IO.File.WriteAllBytes(SkillFile(t.folder), t.original);
                    reg[copy] = new Entry { Name = copy, Enabled = true, Added = DateTime.Now, Source = "Bản sửa của " + t.baseName + " (sửa ngoài plugin)", EditOf = t.folder };
                    _pendingSwitches.Add((copy, t.folder));
                    changed = true;
                    Logger.Info($"skill {t.folder}: edited outside the plugin, edit moved to {copy}, original put back");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"skill {t.folder}: edit outside the plugin not split: {ex.Message}");
                }
            }
            return changed;
        }

        /// <summary>
        /// Copies that just got their first real edit (or were split from an outside edit): (copy, original) pairs,
        /// each reported once, so the current chat can move from the original to the copy.
        /// </summary>
        public List<(string copy, string original)> TakeSwitches()
        {
            lock (_gate)
            {
                var list = new List<(string copy, string original)>(_pendingSwitches);
                _pendingSwitches.Clear();
                var reg = LoadRegistry();
                bool changed = false;
                foreach (var e in reg.Values.Where(x => x.EditOf != null && x.EditSha != null))
                {
                    var h = TextHash(SkillFile(e.Name));
                    if (h == null || h == e.EditSha) continue;
                    list.Add((e.Name, e.EditOf));
                    e.EditSha = null; // edited: a copy of the user's for good
                    changed = true;
                }
                if (changed) SaveRegistry(reg.Values);
                return list;
            }
        }

        /// <summary>Copies made by "Sửa" that were opened and closed without a change: removed (plugin start).</summary>
        public List<string> DiscardUntouchedCopies()
        {
            lock (_gate)
            {
                var gone = new List<string>();
                var reg = LoadRegistry();
                foreach (var e in reg.Values.Where(x => x.EditOf != null && x.EditSha != null && IsValidName(x.Name)).ToList())
                {
                    if (TextHash(SkillFile(e.Name)) != e.EditSha) continue;
                    try
                    {
                        Directory.Delete(Path.Combine(Root, e.Name), true);
                        reg.Remove(e.Name);
                        gone.Add(e.Name);
                    }
                    catch (Exception ex) { Logger.Warn($"untouched copy {e.Name} not removed: {ex.Message}"); }
                }
                if (gone.Count > 0) SaveRegistry(reg.Values);
                return gone;
            }
        }
    }
}
