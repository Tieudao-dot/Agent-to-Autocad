using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>One skill of a pack the owner assigned to this machine in the Google Sheet ("Gán skill").</summary>
    public sealed class PackSkill
    {
        public const string Add = "add", Hide = "hide", Delete = "delete";
        public string Name;   // name on the sheet (the folder may be "<name>-admin", see SkillStore.ApplyPack)
        public string Act;    // add | hide | delete
        public string Rev = "";
        public string Sha256 = ""; // base64url SHA-256 of the UTF-8 bytes of Md
        public string Md;     // SKILL.md (null for delete)
    }

    public sealed class SkillPackCheck
    {
        public bool Ok;
        public string Reason = "";
        public string Machine = "";
        public string Issued = ""; // yyyyMMddHHmmss UTC, set by the service
        public List<PackSkill> Skills = new List<PackSkill>();
        public string Digest = "";
    }

    /// <summary>
    /// Skills pushed per machine (v0.9.6, docs/ke-hoach-day-skill.md). The activation service answers
    /// ?action=status&amp;sk=&lt;digest&gt; of an Active machine with an extra signed line when the machine's pack changed:
    ///   CCP1|machine|yyyyMMddHHmmss|base64url(JSON body)|base64url(RSA-SHA256 of the text before the last |)
    /// Same key pair as CCL1 / CCS1; the prefix keeps one kind of line from being used as another. A skill is an
    /// instruction Claude follows on the customer's drawing, so nothing unsigned is ever written to the library.
    /// </summary>
    public static class SkillPackLogic
    {
        public const string Prefix = "CCP1";
        public const int MaxSkills = 20;
        public const int MaxSkillChars = 45000;   // a Google Sheets cell holds 50 000
        public const int MaxBodyBytes = 400_000;
        public const int MaxNameLength = 58;      // room for the "-admin" suffix within SkillStore's 64
        public const string AdminSuffix = "-admin";
        public const string NoPack = "-";         // sk= of a machine that never received a pack
        private static readonly Regex IssuedRx = new Regex("^[0-9]{14}$");

        public static SkillPackCheck Verify(string line, string machine, string lastIssued) =>
            Verify(line, machine, lastIssued, LicenseLogic.PublicModulus, LicenseLogic.PublicExponent);

        /// <summary>lastIssued: Issued of the last pack this machine accepted ("" = none) — older or equal is a replay.</summary>
        public static SkillPackCheck Verify(string line, string machine, string lastIssued, string modulusB64, string exponentB64)
        {
            var r = new SkillPackCheck();
            line = (line ?? "").Trim();
            if (line.Length == 0) { r.Reason = "không có gói skill"; return r; }
            var parts = line.Split('|');
            if (parts.Length != 5 || parts[0] != Prefix) { r.Reason = "gói skill không đúng định dạng"; return r; }
            r.Machine = parts[1];
            r.Issued = parts[2];
            if (!string.Equals(parts[1], machine, StringComparison.OrdinalIgnoreCase))
            { r.Reason = $"gói skill của máy khác ({parts[1]})"; return r; }
            if (!IssuedRx.IsMatch(parts[2])) { r.Reason = "thời điểm trong gói skill hỏng"; return r; }
            byte[] sig, body;
            try { sig = LicenseLogic.FromBase64Url(parts[4]); body = LicenseLogic.FromBase64Url(parts[3]); }
            catch { r.Reason = "gói skill hỏng (base64)"; return r; }
            if (body.Length > MaxBodyBytes) { r.Reason = $"gói skill quá lớn ({body.Length / 1024} KB, tối đa {MaxBodyBytes / 1000} KB)"; return r; }
            var signed = Encoding.UTF8.GetBytes(line.Substring(0, line.LastIndexOf('|')));
            if (!LicenseLogic.VerifyRsa(signed, sig, modulusB64, exponentB64)) { r.Reason = "chữ ký gói skill không hợp lệ"; return r; }
            if (!string.IsNullOrEmpty(lastIssued) && string.CompareOrdinal(parts[2], lastIssued) <= 0)
            { r.Reason = "gói skill cũ hơn gói đã nhận (bị gửi lại?)"; return r; }

            Dictionary<string, object> d;
            try { d = JsonCodec.ParseObject(Encoding.UTF8.GetString(body)); }
            catch { r.Reason = "nội dung gói skill không phải JSON"; return r; }
            if (!(d.TryGetValue("v", out var v) && v is double dv && dv == 1)) { r.Reason = "phiên bản gói skill không hỗ trợ"; return r; }
            if (!(d.TryGetValue("skills", out var sv) && sv is List<object> list)) { r.Reason = "gói skill thiếu danh sách"; return r; }
            if (list.Count > MaxSkills) { r.Reason = $"gói có {list.Count} skill, tối đa {MaxSkills}"; return r; }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var o in list)
            {
                if (!(o is Dictionary<string, object> m)) { r.Reason = "mục skill trong gói hỏng"; return r; }
                var s = new PackSkill
                {
                    Name = Str(m, "name"),
                    Act = Str(m, "act"),
                    Rev = Str(m, "rev") ?? "",
                    Sha256 = Str(m, "sha256") ?? "",
                    Md = Str(m, "md"),
                };
                var why = Check(s);
                if (why != null) { r.Reason = $"skill \"{s.Name}\": {why}"; return r; }
                if (!seen.Add(s.Name)) { r.Reason = $"skill \"{s.Name}\" có hai lần trong gói"; return r; }
                r.Skills.Add(s);
            }
            r.Digest = Digest(r.Skills);
            r.Ok = true;
            return r;
        }

        /// <summary>Why one skill of a pack cannot be used, or null.</summary>
        public static string Check(PackSkill s)
        {
            if (!SkillStore.IsValidName(s.Name) || s.Name.Length > MaxNameLength || s.Name.EndsWith(AdminSuffix, StringComparison.Ordinal))
                return "tên không hợp lệ";
            if (s.Act == PackSkill.Delete) return null;
            if (s.Act != PackSkill.Add && s.Act != PackSkill.Hide) return "hành động lạ";
            if (string.IsNullOrEmpty(s.Md)) return "thiếu nội dung";
            if (s.Md.Length > MaxSkillChars) return $"dài {s.Md.Length} ký tự, tối đa {MaxSkillChars}";
            if (Sha256(s.Md) != s.Sha256) return "mã băm không khớp nội dung";
            var (name, _, error) = SkillStore.ParseFrontmatter(s.Md);
            if (error != null) return error;
            if (name != s.Name) return $"name trong nội dung là \"{name}\"";
            return null;
        }

        /// <summary>sk= sent with ?action=status: SHA-256 (base64url) of "name:act:rev:sha256\n" sorted by name.</summary>
        public static string Digest(IEnumerable<PackSkill> skills)
        {
            var sb = new StringBuilder();
            foreach (var s in (skills ?? Enumerable.Empty<PackSkill>()).OrderBy(k => k.Name, StringComparer.Ordinal))
                sb.Append(s.Name).Append(':').Append(s.Act).Append(':').Append(s.Rev).Append(':').Append(s.Act == PackSkill.Delete ? "" : s.Sha256).Append('\n');
            return Sha256(sb.ToString());
        }

        public static string Sha256(string text) => Sha256(Encoding.UTF8.GetBytes(text ?? ""));

        public static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string Str(Dictionary<string, object> m, string k) => m.TryGetValue(k, out var v) ? v as string : null;
    }
}
