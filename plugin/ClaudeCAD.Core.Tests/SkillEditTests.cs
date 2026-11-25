using System;
using System.IO;
using System.Linq;
using System.Text;
using ClaudeCAD.Plugin.Core;
using Xunit;

namespace ClaudeCAD.Core.Tests
{
    /// <summary>v0.9.6: editing a Default / Admin skill makes a You copy; the original is never written.</summary>
    public class SkillEditTests
    {
        private static string Temp()
        {
            var d = Path.Combine(Path.GetTempPath(), "ccedit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(d);
            return d;
        }

        private static string Md(string name, string body = "Bản gốc.") =>
            $"---\nname: {name}\ndescription: Chuẩn vẽ điện nước của công ty - layer, ký hiệu. Dùng khi vẽ MEP.\n---\n\n# {name}\n\n{body}";

        private static void Put(string root, string name, string md, string extra = null)
        {
            Directory.CreateDirectory(Path.Combine(root, name));
            File.WriteAllText(Path.Combine(root, name, "SKILL.md"), md, new UTF8Encoding(false));
            if (extra != null) File.WriteAllText(Path.Combine(root, name, "bang.csv"), extra);
        }

        /// <summary>Library with the sample ve-mep (as shipped) and its pristine copy next to it.</summary>
        private static SkillStore WithSample()
        {
            var top = Temp();
            var root = Path.Combine(top, "skills");
            var pristine = Path.Combine(top, SampleSkills.PristineFolderName);
            Put(root, "ve-mep", Md("ve-mep"), "a,b");
            Put(pristine, "ve-mep", Md("ve-mep"), "a,b");
            return new SkillStore(root) { PristineDir = pristine };
        }

        private static PackSkill P(string name, string md) =>
            new PackSkill { Name = name, Act = "add", Rev = "1", Md = md, Sha256 = SkillPackLogic.Sha256(md) };

        [Fact]
        public void EditingADefaultSkillCopiesItAndNeverWritesTheOriginal()
        {
            var store = WithSample();
            var (open, copied) = store.PrepareEdit("ve-mep");
            Assert.True(copied);
            Assert.Equal("ve-mep-sua", open);
            Assert.Contains("name: ve-mep-sua", File.ReadAllText(store.SkillFile(open)));
            Assert.True(File.Exists(Path.Combine(store.Root, open, "bang.csv"))); // reference files come along
            var list = store.Scan();
            var copy = list.Single(k => k.Name == "ve-mep-sua");
            Assert.Equal("You", copy.Label);
            Assert.Equal("ve-mep", copy.EditOf);
            Assert.Null(copy.Error);
            Assert.Equal("Default", list.Single(k => k.Name == "ve-mep").Label);

            // the existing copy is opened again, no second copy
            Assert.Equal(("ve-mep-sua", false), store.PrepareEdit("ve-mep"));
            Assert.Equal(2, store.Scan().Count);
            // a You skill opens as it is
            Assert.Equal(("ve-mep-sua", false), store.PrepareEdit("ve-mep-sua"));
        }

        [Fact]
        public void AnUntouchedCopyIsDiscardedAnEditedOneSwitchesOnce()
        {
            var store = WithSample();
            store.PrepareEdit("ve-mep");
            Assert.Empty(store.TakeSwitches());          // opened, nothing saved yet
            File.WriteAllText(store.SkillFile("ve-mep-sua"), File.ReadAllText(store.SkillFile("ve-mep-sua")).Replace("\n", "\r\n")); // only line endings
            Assert.Empty(store.TakeSwitches());
            Assert.Equal(new[] { "ve-mep-sua" }, store.DiscardUntouchedCopies());
            Assert.False(Directory.Exists(Path.Combine(store.Root, "ve-mep-sua")));

            store.PrepareEdit("ve-mep");
            File.WriteAllText(store.SkillFile("ve-mep-sua"), Md("ve-mep-sua", "Khách sửa."));
            Assert.Equal(new[] { ("ve-mep-sua", "ve-mep") }, store.TakeSwitches());
            Assert.Empty(store.TakeSwitches());          // reported once
            Assert.Empty(store.DiscardUntouchedCopies()); // edited copies stay
            Assert.Contains("Bản gốc.", File.ReadAllText(store.SkillFile("ve-mep")));
        }

        [Fact]
        public void EditingAnAdminSkillKeepsItAdminAndStillUpdatable()
        {
            var store = new SkillStore(Temp());
            store.ApplyPack(new[] { P("cty-a", Md("cty-a")) });
            var (open, copied) = store.PrepareEdit("cty-a");
            Assert.True(copied);
            Assert.Equal("cty-a-sua", open);
            File.WriteAllText(store.SkillFile(open), Md("cty-a-sua", "Khách sửa."));
            var list = store.Scan();
            Assert.Equal("Admin", list.Single(k => k.Name == "cty-a").Label);
            Assert.Equal("You", list.Single(k => k.Name == "cty-a-sua").Label);

            // the owner's next version still reaches the original; hide / delete too; the copy is untouched
            store.ApplyPack(new[] { new PackSkill { Name = "cty-a", Act = "add", Rev = "2", Md = Md("cty-a", "Bản 2."), Sha256 = SkillPackLogic.Sha256(Md("cty-a", "Bản 2.")) } });
            Assert.Contains("Bản 2.", File.ReadAllText(store.SkillFile("cty-a")));
            store.ApplyPack(new[] { new PackSkill { Name = "cty-a", Act = "delete" } });
            Assert.False(Directory.Exists(Path.Combine(store.Root, "cty-a")));
            Assert.Contains("Khách sửa.", File.ReadAllText(store.SkillFile("cty-a-sua")));
        }

        [Fact]
        public void AnAdminSkillStoredAsDashAdminIsCopiedUnderItsSheetName()
        {
            var store = new SkillStore(Temp());
            Put(store.Root, "cty-a", Md("cty-a", "Của khách."));
            store.ApplyPack(new[] { P("cty-a", Md("cty-a")) });
            Assert.Equal(("cty-a-sua", true), store.PrepareEdit("cty-a-admin"));
            Assert.Equal("cty-a-admin", store.Scan().Single(k => k.Name == "cty-a-sua").EditOf);
        }

        [Fact]
        public void AnAdminSkillEditedOutsideThePluginIsSplitAndPutBack()
        {
            var store = new SkillStore(Temp());
            store.ApplyPack(new[] { P("cty-a", Md("cty-a")) });
            File.WriteAllText(store.SkillFile("cty-a"), Md("cty-a", "Sửa bằng Notepad."));
            var list = store.Scan();
            var admin = list.Single(k => k.Name == "cty-a");
            Assert.True(admin.Admin);
            Assert.Contains("Bản gốc.", File.ReadAllText(store.SkillFile("cty-a")));
            var copy = list.Single(k => k.Name == "cty-a-sua");
            Assert.Equal("You", copy.Label);
            Assert.Contains("Sửa bằng Notepad.", File.ReadAllText(store.SkillFile("cty-a-sua")));
            Assert.Contains("name: cty-a-sua", File.ReadAllText(store.SkillFile("cty-a-sua")));
            Assert.Equal(new[] { ("cty-a-sua", "cty-a") }, store.TakeSwitches());

            // a second outside edit never overwrites the first copy
            File.WriteAllText(store.SkillFile("cty-a"), Md("cty-a", "Lần hai."));
            store.Scan();
            Assert.Contains("Sửa bằng Notepad.", File.ReadAllText(store.SkillFile("cty-a-sua")));
            Assert.Contains("Lần hai.", File.ReadAllText(store.SkillFile("cty-a-sua-2")));
            Assert.Contains("Bản gốc.", File.ReadAllText(store.SkillFile("cty-a")));
        }

        [Fact]
        public void ADefaultSkillEditedOutsideThePluginIsSplitAndRestoredFromTheSamples()
        {
            var store = WithSample();
            File.WriteAllText(store.SkillFile("ve-mep"), Md("ve-mep", "Sửa bằng Explorer."));
            var list = store.Scan();
            Assert.Contains("Bản gốc.", File.ReadAllText(store.SkillFile("ve-mep")));
            Assert.Equal("ve-mep", list.Single(k => k.Name == "ve-mep-sua").EditOf);
            Assert.Contains("Sửa bằng Explorer.", File.ReadAllText(store.SkillFile("ve-mep-sua")));
            Assert.Equal(new[] { ("ve-mep-sua", "ve-mep") }, store.TakeSwitches());
        }

        [Fact]
        public void AnOlderUntouchedSampleIsNotTakenForAnEdit()
        {
            var store = WithSample();
            var older = Md("ve-mep", "Bản cũ của mẫu.");
            File.WriteAllText(store.SkillFile("ve-mep"), older);
            var known = new[] { "ve-mep/SKILL.md|" + SampleSkills.Hash(Encoding.UTF8.GetBytes(older)) };
            Assert.False(SampleSkills.IsEdited(store.PristineDir, store.Root, "ve-mep/SKILL.md", known));
            Assert.True(SampleSkills.IsEdited(store.PristineDir, store.Root, "ve-mep/SKILL.md", new string[0]));
            // without the pristine samples (dev checkout) nothing is split
            var bare = new SkillStore(store.Root);
            File.WriteAllText(store.SkillFile("ve-mep"), Md("ve-mep", "Khác."));
            Assert.DoesNotContain(bare.Scan(), k => k.Name == "ve-mep-sua");
        }
    }
}
