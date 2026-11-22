using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ClaudeCAD.Plugin.Core;
using Xunit;

namespace ClaudeCAD.Core.Tests
{
    public class SkillPackTests
    {
        private const string Machine = "CC-1A2B-3C4D-5E6F";
        private static readonly RSA Key = RSA.Create(2048); // throw-away, made for this test run
        private static string Mod => Convert.ToBase64String(Key.ExportParameters(false).Modulus);
        private static string Exp => Convert.ToBase64String(Key.ExportParameters(false).Exponent);

        private static string B64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static string Sign(string payload) =>
            payload + "|" + B64Url(Key.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        private static string Md(string name, string body = "Nội dung.") =>
            $"---\nname: {name}\ndescription: Chuẩn vẽ của công ty A - layer, chữ, kích thước. Dùng khi vẽ cho công ty A.\n---\n\n# {name}\n\n{body}";

        private static Dictionary<string, object> Skill(string name, string act = "add", string rev = "1", string md = null)
        {
            var d = new Dictionary<string, object> { ["name"] = name, ["act"] = act, ["rev"] = rev };
            if (act != "delete")
            {
                md = md ?? Md(name);
                d["md"] = md;
                d["sha256"] = SkillPackLogic.Sha256(md);
            }
            return d;
        }

        private static string Pack(string issued, params Dictionary<string, object>[] skills) =>
            PackFor(Machine, issued, skills);

        private static string PackFor(string machine, string issued, params Dictionary<string, object>[] skills)
        {
            var body = JsonCodec.Serialize(new Dictionary<string, object> { ["v"] = 1, ["skills"] = skills.Cast<object>().ToList() });
            return Sign("CCP1|" + machine + "|" + issued + "|" + B64Url(Encoding.UTF8.GetBytes(body)));
        }

        private static SkillPackCheck Verify(string line, string last = "") => SkillPackLogic.Verify(line, Machine, last, Mod, Exp);

        [Fact]
        public void AcceptsASignedPackForThisMachine()
        {
            var r = Verify(Pack("20261122103000", Skill("cty-a-ket-cau"), Skill("cty-a-cu", "delete")));
            Assert.True(r.Ok, r.Reason);
            Assert.Equal("20261122103000", r.Issued);
            Assert.Equal(new[] { "cty-a-ket-cau", "cty-a-cu" }, r.Skills.Select(s => s.Name));
            Assert.Equal("delete", r.Skills[1].Act);
            Assert.Equal(SkillPackLogic.Digest(r.Skills), r.Digest);
            Assert.True(SkillPackLogic.Verify(Pack("20261122103000"), "cc-1a2b-3c4d-5e6f", "", Mod, Exp).Ok); // empty pack, lower-case machine
        }

        [Fact]
        public void RejectsForgedReplayedAndForeignPacks()
        {
            var line = Pack("20261122103000", Skill("cty-a-ket-cau"));
            // another machine's pack
            Assert.False(SkillPackLogic.Verify(PackFor("CC-9999-9999-9999", "20261122103000", Skill("x-y")), Machine, "", Mod, Exp).Ok);
            // body swapped, signature kept
            var parts = line.Split('|');
            var other = Pack("20261122103000", Skill("cty-b"));
            Assert.False(Verify(parts[0] + "|" + parts[1] + "|" + parts[2] + "|" + other.Split('|')[3] + "|" + parts[4]).Ok);
            // replay: same or older than the last accepted pack
            Assert.False(Verify(line, "20261122103000").Ok);
            Assert.False(Verify(line, "20261123000000").Ok);
            Assert.True(Verify(line, "20261121000000").Ok);
            // the real public key does not accept a test signature
            Assert.False(SkillPackLogic.Verify(line, Machine, "").Ok);
            // wrong prefix / shape / garbage
            Assert.False(Verify("CCS1" + line.Substring(4)).Ok);
            Assert.False(Verify(line + "|x").Ok);
            Assert.False(Verify("").Ok);
            Assert.False(Verify(null).Ok);
            Assert.False(Verify(Sign("CCP1|" + Machine + "|2026112210|" + B64Url(Encoding.UTF8.GetBytes("{\"v\":1,\"skills\":[]}")))).Ok); // bad time
            Assert.False(Verify(Sign("CCP1|" + Machine + "|20261122103000|" + B64Url(Encoding.UTF8.GetBytes("not json")))).Ok);
            Assert.False(Verify(Sign("CCP1|" + Machine + "|20261122103000|" + B64Url(Encoding.UTF8.GetBytes("{\"v\":2,\"skills\":[]}")))).Ok);
        }

        [Fact]
        public void StatusAndLicenceSignaturesCannotPassAsAPack()
        {
            // a CCS1 line signed with the same key: different prefix and shape
            var status = Sign("CCS1|" + Machine + "|active|2026-11-22");
            Assert.False(Verify(status).Ok);
            Assert.False(LicenseLogic.VerifyStatus(Pack("20261122103000"), Machine, DateTime.UtcNow, Mod, Exp).Ok);
        }

        [Fact]
        public void EnforcesLimitsAndSkillRules()
        {
            Assert.False(Verify(Pack("20261122103000", Enumerable.Range(0, 21).Select(i => Skill("s-" + i)).ToArray())).Ok);
            Assert.True(Verify(Pack("20261122103000", Enumerable.Range(0, 20).Select(i => Skill("s-" + i)).ToArray())).Ok);
            Assert.False(Verify(Pack("20261122103000", Skill("dai", md: Md("dai", new string('x', 45001))))).Ok);
            var bad = Skill("cty-a");
            bad["sha256"] = SkillPackLogic.Sha256("khác");
            Assert.False(Verify(Pack("20261122103000", bad)).Ok);
            Assert.False(Verify(Pack("20261122103000", Skill("Ten Sai"))).Ok);
            Assert.False(Verify(Pack("20261122103000", Skill("abc-admin"))).Ok);
            Assert.False(Verify(Pack("20261122103000", Skill(new string('a', 59)))).Ok);
            Assert.False(Verify(Pack("20261122103000", Skill("cty-a", md: Md("cty-b")))).Ok); // frontmatter name differs
            Assert.False(Verify(Pack("20261122103000", Skill("cty-a", md: "không có frontmatter"))).Ok);
            Assert.False(Verify(Pack("20261122103000", Skill("cty-a", act: "run"))).Ok);
            Assert.False(Verify(Pack("20261122103000", Skill("cty-a"), Skill("cty-a", "hide"))).Ok); // twice
        }

        [Fact]
        public void DigestIsOrderIndependentAndIgnoresContentOfDeletes()
        {
            var a = new PackSkill { Name = "a", Act = "add", Rev = "1", Sha256 = "x" };
            var b = new PackSkill { Name = "b", Act = "delete", Rev = "2", Sha256 = "ignored" };
            Assert.Equal(SkillPackLogic.Digest(new[] { a, b }), SkillPackLogic.Digest(new[] { b, a }));
            Assert.Equal(SkillPackLogic.Digest(new[] { a, b }), SkillPackLogic.Digest(new[] { a, new PackSkill { Name = "b", Act = "delete", Rev = "2" } }));
            Assert.NotEqual(SkillPackLogic.Digest(new[] { a }), SkillPackLogic.Digest(new[] { new PackSkill { Name = "a", Act = "hide", Rev = "1", Sha256 = "x" } }));
            // fixed vector: activation/apps-script/Code.gs.template must compute the same (agent/test/activation.test.ts)
            Assert.Equal("47DEQpj8HBSa-_TImW-5JCeuQeRkm5NMpJWZG3hSuFU", SkillPackLogic.Digest(new PackSkill[0]));
            Assert.Equal("WTqycN8fUlMedhP9nVCTcvC6bt2w0vb44eR7weJPb-w", SkillPackLogic.Digest(new[]
            {
                new PackSkill { Name = "b", Act = "delete", Rev = "2", Sha256 = "ignored" },
                new PackSkill { Name = "a", Act = "add", Rev = "1", Sha256 = SkillPackLogic.Sha256("x") },
            }));
        }

        // ------------------------------------------------------------------ SkillStore.ApplyPack

        private static string Temp()
        {
            var d = Path.Combine(Path.GetTempPath(), "ccpack-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(d);
            return d;
        }

        private static PackSkill P(string name, string act = "add", string rev = "1", string md = null)
        {
            md = act == "delete" ? null : md ?? Md(name);
            return new PackSkill { Name = name, Act = act, Rev = rev, Md = md, Sha256 = md == null ? "" : SkillPackLogic.Sha256(md) };
        }

        [Fact]
        public void AddsUpdatesHidesShowsAndDeletesAdminSkills()
        {
            var store = new SkillStore(Temp());
            var c = store.ApplyPack(new[] { P("cty-a") });
            Assert.Equal(PackChangeKind.Added, Assert.Single(c).Kind);
            var s = Assert.Single(store.Scan());
            Assert.True(s.Admin);
            Assert.Equal("Admin", s.Label);
            Assert.False(s.Hidden);
            Assert.Empty(SkillSelection.Choice(null, null, store.Scan())); // never turned on by itself

            Assert.Empty(store.ApplyPack(new[] { P("cty-a") })); // same: nothing to do
            c = store.ApplyPack(new[] { P("cty-a", rev: "2", md: Md("cty-a", "Bản 2.")) });
            Assert.Equal(PackChangeKind.Updated, Assert.Single(c).Kind);
            Assert.Contains("Bản 2.", File.ReadAllText(store.SkillFile("cty-a")));
            Assert.True(store.Scan().Single().Admin); // our own write is not a user edit

            c = store.ApplyPack(new[] { P("cty-a", "hide", "2", Md("cty-a", "Bản 2.")) });
            Assert.Equal(PackChangeKind.Hidden, Assert.Single(c).Kind);
            Assert.True(store.Scan().Single().Hidden);
            Assert.True(File.Exists(store.SkillFile("cty-a")));
            Assert.Empty(SkillSelection.ForChat(new List<string> { "cty-a" }, null, store.Scan()));
            Assert.Contains("\"hidden\": true", File.ReadAllText(store.RegistryPath).Replace("\"hidden\":true", "\"hidden\": true"));

            c = store.ApplyPack(new[] { P("cty-a", "add", "2", Md("cty-a", "Bản 2.")) });
            Assert.Equal(PackChangeKind.Shown, Assert.Single(c).Kind);
            Assert.Equal(new[] { "cty-a" }, SkillSelection.ForChat(new List<string> { "cty-a" }, null, store.Scan()));

            Assert.Empty(store.ApplyPack(new PackSkill[0])); // missing from the pack: kept as it is
            Assert.Single(store.Scan());

            c = store.ApplyPack(new[] { P("cty-a", "delete") });
            Assert.Equal(PackChangeKind.Deleted, Assert.Single(c).Kind);
            Assert.Empty(store.Scan());
            Assert.Empty(store.ApplyPack(new[] { P("cty-a", "delete") }));
        }

        [Fact]
        public void KeepsBothWhenTheNameIsTakenAndNeverTouchesYouOrDefaultSkills()
        {
            var root = Temp();
            var store = new SkillStore(root);
            Directory.CreateDirectory(Path.Combine(root, "cty-a"));
            File.WriteAllText(Path.Combine(root, "cty-a", "SKILL.md"), Md("cty-a", "Của khách."));
            Directory.CreateDirectory(Path.Combine(root, "ve-mep"));
            File.WriteAllText(Path.Combine(root, "ve-mep", "SKILL.md"), Md("ve-mep", "Mẫu."));

            var c = store.ApplyPack(new[] { P("cty-a"), P("ve-mep") });
            Assert.Equal(new[] { "cty-a-admin", "ve-mep-admin" }, c.Select(x => x.Folder).OrderBy(x => x));
            var list = store.Scan();
            Assert.Equal("You", list.Single(k => k.Name == "cty-a").Label);
            Assert.Equal("Default", list.Single(k => k.Name == "ve-mep").Label);
            var admin = list.Single(k => k.Name == "cty-a-admin");
            Assert.Equal("Admin", admin.Label);
            Assert.Equal("cty-a", admin.DisplayName);
            Assert.Null(admin.Error); // frontmatter name follows the folder
            Assert.Contains("Của khách.", File.ReadAllText(store.SkillFile("cty-a")));

            // hide / delete reach only the Admin copy
            store.ApplyPack(new[] { P("cty-a", "delete"), P("ve-mep", "hide") });
            list = store.Scan();
            Assert.True(File.Exists(store.SkillFile("cty-a")));
            Assert.False(Directory.Exists(Path.Combine(root, "cty-a-admin")));
            Assert.False(list.Single(k => k.Name == "ve-mep").Hidden);
            Assert.True(list.Single(k => k.Name == "ve-mep-admin").Hidden);

            // both names taken by the user: skipped, nothing overwritten
            Directory.CreateDirectory(Path.Combine(root, "cty-a-admin"));
            File.WriteAllText(Path.Combine(root, "cty-a-admin", "SKILL.md"), Md("cty-a-admin", "Cũng của khách."));
            c = store.ApplyPack(new[] { P("cty-a", rev: "5") });
            Assert.Equal(PackChangeKind.Skipped, Assert.Single(c).Kind);
            Assert.Contains("Cũng của khách.", File.ReadAllText(store.SkillFile("cty-a-admin")));
        }

        [Fact]
        public void AnAdminSkillEditedByTheUserBecomesTheirs()
        {
            var store = new SkillStore(Temp());
            store.ApplyPack(new[] { P("cty-a") });
            File.WriteAllText(store.SkillFile("cty-a"), Md("cty-a", "Khách sửa."));
            var s = store.Scan().Single();
            Assert.False(s.Admin);
            Assert.Equal("You", s.Label);
            Assert.DoesNotContain("origin", File.ReadAllText(store.RegistryPath));

            // later owner actions do not touch it; a newer owner version arrives next to it
            Assert.Empty(store.ApplyPack(new[] { P("cty-a", "delete") }));
            Assert.True(File.Exists(store.SkillFile("cty-a")));
            var c = store.ApplyPack(new[] { P("cty-a", rev: "2") });
            Assert.Equal("cty-a-admin", Assert.Single(c).Folder);
            Assert.Contains("Khách sửa.", File.ReadAllText(store.SkillFile("cty-a")));
        }

        [Fact]
        public void RegistryKeepsOwnerFieldsAcrossScans()
        {
            var store = new SkillStore(Temp());
            store.ApplyPack(new[] { P("cty-a", "hide", "7") });
            store.Scan();
            store.SetEnabled("cty-a", true);
            var s = new SkillStore(store.Root).Scan().Single();
            Assert.True(s.Admin);
            Assert.True(s.Hidden);
            Assert.Equal("cty-a", s.AdminName);
            Assert.Contains("\"rev\"", File.ReadAllText(store.RegistryPath));
        }
    }
}
