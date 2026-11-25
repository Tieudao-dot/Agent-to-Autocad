using System;
using System.IO;
using System.Linq;
using System.Text;
using ClaudeCAD.Plugin.Core;
using Xunit;

namespace ClaudeCAD.Core.Tests
{
    /// <summary>v0.9.8: "Sửa ▸ ứng dụng .md / Chọn ứng dụng khác… / Word", working copies and "Tạo skill mới".</summary>
    public class SkillEditSessionTests
    {
        private const string Body =
            "# Vẽ MEP\n\nDùng **đúng** layer E-*, P-* và *luôn* gọi `get_drawing_info` trước.\n\n" +
            "## Quy trình\n\n- Bước một\n  - Bước con\n1. Đánh số\n\n" +
            "## Bảng layer chuẩn\n\n| Layer | Nội dung | Màu | Linetype | Lineweight |\n| --- | --- | --- | --- | --- |\n" +
            "| `E-LIGHT` | Đèn | 2 | Continuous | 0.25 |\n| `P-PIPE` | Ống | 4 | Continuous | 0.35 |\n";

        private static string Md(string name, string body = Body) =>
            $"---\nname: {name}\ndescription: Chuẩn vẽ MEP của công ty A - layer E-*, P-*. Dùng khi vẽ điện nước.\n---\n\n{body}";

        private static string Temp()
        {
            var d = Path.Combine(Path.GetTempPath(), "ccsess-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(d);
            return d;
        }

        private static (SkillStore store, string work) Library()
        {
            var top = Temp();
            var root = Path.Combine(top, "skills");
            foreach (var n in new[] { "ve-mep", "cua-toi" })
            {
                Directory.CreateDirectory(Path.Combine(root, n));
                File.WriteAllText(Path.Combine(root, n, "SKILL.md"), Md(n), new UTF8Encoding(false));
            }
            return (new SkillStore(root), Path.Combine(top, "skill-edit"));
        }

        private static void SaveInWord(string docx, string body) => File.WriteAllBytes(docx, MarkdownToDocx.Convert(body));

        [Fact]
        public void MarkdownGoesToWordAndBackWithItsStructureAndLayerTable()
        {
            var back = DocxToMarkdown.Convert(new MemoryStream(MarkdownToDocx.Convert(Body)));
            Assert.Null(back.Error);
            Assert.Empty(back.Dropped);
            var md = back.Markdown;
            Assert.Contains("# Vẽ MEP\n", md);
            Assert.Contains("Dùng **đúng** layer E-*, P-* và *luôn* gọi `get_drawing_info` trước.", md);
            Assert.Contains("## Quy trình\n", md);
            Assert.Contains("- Bước một\n  - Bước con\n1. Đánh số\n", md);
            Assert.Contains("| Layer | Nội dung | Màu | Linetype | Lineweight |", md);
            Assert.Contains("| `E-LIGHT` | Đèn | 2 | Continuous | 0.25 |", md);
            Assert.Null(SkillStore.StandardWarning(Md("x", md)));
            // the same text twice through Word does not drift
            var again = DocxToMarkdown.Convert(new MemoryStream(MarkdownToDocx.Convert(md))).Markdown;
            Assert.Equal(md, again);
        }

        [Fact]
        public void AYouSkillInAnAppIsEditedInPlace()
        {
            var (store, work) = Library();
            var s = SkillEditSession.Start(store, "cua-toi", SkillEditMode.App, work);
            Assert.True(s.InPlace);
            Assert.Equal(store.SkillFile("cua-toi"), s.WorkFile);
            Assert.Equal("cua-toi", s.Target);
            Assert.False(Directory.Exists(work));
        }

        [Fact]
        public void AYouSkillInWordKeepsItsFrontmatterAndIsOverwritten()
        {
            var (store, work) = Library();
            var s = SkillEditSession.Start(store, "cua-toi", SkillEditMode.Word, work);
            Assert.EndsWith(".docx", s.WorkFile);
            Assert.False(s.Read().Changed); // opened and saved without a change
            SaveInWord(s.WorkFile, Body.Replace("Đèn", "Đèn chiếu sáng"));
            var r = s.Read();
            Assert.True(r.Changed);
            Assert.Null(r.Warning);
            Assert.Null(store.SaveBody(s.Target, r.Body));
            s.Accept(r);
            Assert.False(s.Read().Changed);
            var saved = File.ReadAllText(store.SkillFile("cua-toi"));
            Assert.StartsWith("---\nname: cua-toi\ndescription: Chuẩn vẽ MEP", saved);
            Assert.Contains("Đèn chiếu sáng", saved);
            Assert.Equal("You", store.LabelOf("cua-toi"));
        }

        [Fact]
        public void ABrokenLayerTableFromWordIsReported()
        {
            var (store, work) = Library();
            var s = SkillEditSession.Start(store, "cua-toi", SkillEditMode.Word, work);
            SaveInWord(s.WorkFile, Body.Replace("| `E-LIGHT` | Đèn | 2 |", "| `E-LIGHT` | Đèn | đỏ tươi |"));
            var r = s.Read();
            Assert.True(r.Changed);
            Assert.Contains("Bảng layer chuẩn", r.Warning);
        }

        [Fact]
        public void ADefaultSkillIsEditedInACopyAndBecomesANewSkillOnlyWhenTheUserSaysSo()
        {
            var (store, work) = Library();
            var before = File.ReadAllText(store.SkillFile("ve-mep"));
            var s = SkillEditSession.Start(store, "ve-mep", SkillEditMode.App, work);
            Assert.False(s.InPlace);
            Assert.Null(s.Target);
            Assert.StartsWith(work, s.WorkFile);
            Assert.False(s.Read().Changed);

            File.WriteAllText(s.WorkFile, Md("ve-mep", Body + "\nThêm một dòng.\n"));
            var r = s.Read();
            Assert.True(r.Changed);
            Assert.Equal("ve-mep-sua", r.SuggestedName);
            Assert.StartsWith("Chuẩn vẽ MEP", r.SuggestedDescription);
            Assert.Equal(before, File.ReadAllText(store.SkillFile("ve-mep"))); // the original is never written

            // "Tạo skill mới": name / description checks, then a You skill; the chat follows it
            Assert.NotNull(store.CreateEditedCopy("ve-mep", "cua-toi", r.SuggestedDescription, r.Body).error); // taken
            Assert.NotNull(store.CreateEditedCopy("ve-mep", "Tên Sai !!", "ngắn", r.Body).error);
            Assert.NotNull(store.CreateEditedCopy("ve-mep", "x-admin", r.SuggestedDescription, r.Body).error);
            var (name, error) = store.CreateEditedCopy("ve-mep", "MEP công ty A", r.SuggestedDescription, r.Body);
            Assert.Null(error);
            Assert.Equal("mep-cong-ty-a", name);
            var lib = store.Scan();
            var made = lib.Single(k => k.Name == name);
            Assert.Equal("You", made.Label);
            Assert.Equal("ve-mep", made.EditOf);
            Assert.Null(made.Error);
            Assert.Contains("Thêm một dòng.", File.ReadAllText(store.SkillFile(name)));
            Assert.Equal(new[] { (name, "ve-mep") }, store.TakeSwitches());
            Assert.Equal(before, File.ReadAllText(store.SkillFile("ve-mep")));

            // further saves of the same working copy go to the new skill
            s.Target = name;
            s.Accept(r);
            Assert.False(s.Read().Changed);
        }

        [Fact]
        public void ANameTypedInTheCopysFrontmatterIsSuggested()
        {
            var (store, work) = Library();
            var s = SkillEditSession.Start(store, "ve-mep", SkillEditMode.App, work);
            File.WriteAllText(s.WorkFile, Md("mep-rieng").Replace("description: Chuẩn vẽ MEP", "description: Chuẩn MEP riêng"));
            var r = s.Read();
            Assert.Equal("mep-rieng", r.SuggestedName);
            Assert.StartsWith("Chuẩn MEP riêng", r.SuggestedDescription);
        }

        [Fact]
        public void AnAdminSkillInWordNeverWritesTheOriginal()
        {
            var (store, work) = Library();
            var md = Md("cty-a");
            store.ApplyPack(new[] { new PackSkill { Name = "cty-a", Act = "add", Rev = "1", Md = md, Sha256 = SkillPackLogic.Sha256(md) } });
            var s = SkillEditSession.Start(store, "cty-a", SkillEditMode.Word, work);
            Assert.Equal("Admin", s.Label);
            Assert.Null(s.Target);
            Assert.Throws<InvalidOperationException>(() => store.SaveBody("cty-a", "x"));
            Assert.Throws<InvalidOperationException>(() => store.SaveBody("ve-mep", "x"));
            SaveInWord(s.WorkFile, Body + "\nKhác.\n");
            Assert.True(s.Read().Changed);
            Assert.True(store.Scan().Single(k => k.Name == "cty-a").Admin);
        }

        [Fact]
        public void SplitsTheFrontmatterAndCleansOldWorkFolders()
        {
            Assert.Equal(("---\nname: a\n---\n", "# A\n"), SkillStore.SplitFrontmatter("---\r\nname: a\r\n---\r\n\r\n# A\r\n"));
            Assert.Equal(("", "# không có phần đầu"), SkillStore.SplitFrontmatter("# không có phần đầu"));
            var work = Temp();
            var old = Directory.CreateDirectory(Path.Combine(work, "ve-mep-cu"));
            Directory.SetLastWriteTime(old.FullName, DateTime.Now.AddDays(-10));
            Directory.CreateDirectory(Path.Combine(work, "ve-mep-moi"));
            Assert.Equal(1, SkillEditSession.CleanWorkRoot(work));
            Assert.True(Directory.Exists(Path.Combine(work, "ve-mep-moi")));
        }
    }
}
