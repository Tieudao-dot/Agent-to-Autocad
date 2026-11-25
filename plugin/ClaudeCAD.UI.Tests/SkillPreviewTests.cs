using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ClaudeCAD.Plugin.Core;
using ClaudeCAD.Plugin.UI;
using Xunit;

namespace ClaudeCAD.UI.Tests
{
    /// <summary>v0.9.8: ⌄ on a skill shows its content in the list — read-only, formatted, limited height with a scroll bar.</summary>
    public class SkillPreviewTests
    {
        private static void Sta(Action a)
        {
            Exception error = null;
            var t = new Thread(() =>
            {
                try { a(); }
                catch (Exception ex) { error = ex; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (error != null) throw new Exception("UI test failed: " + error.Message, error);
        }

        private static IEnumerable<object> All(object root)
        {
            var stack = new Stack<object>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var o = stack.Pop();
                yield return o;
                if (o is DependencyObject d)
                    foreach (var c in LogicalTreeHelper.GetChildren(d)) stack.Push(c);
            }
        }

        [Fact]
        public void TheFoldButtonShowsTheSkillReadOnlyWithoutItsFrontmatter()
        {
            var dir = Path.Combine(Path.GetTempPath(), "ccpreview-" + Guid.NewGuid().ToString("N"), "ve-mep");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "SKILL.md");
            var md = "---\nname: ve-mep\ndescription: Chuẩn vẽ MEP mẫu - điện nước, dùng khi vẽ MEP.\n---\n\n# Vẽ MEP\n\n" +
                     "| Layer | Nội dung | Màu | Linetype | Lineweight |\n| --- | --- | --- | --- | --- |\n| `E-LIGHT` | Đèn | 2 | Continuous | 0.25 |\n" +
                     string.Concat(Enumerable.Range(1, 80).Select(i => $"\n- Dòng {i}"));
            File.WriteAllText(file, md, new UTF8Encoding(false));
            var written = File.GetLastWriteTimeUtc(file);
            string shown = null;
            double maxHeight = 0;
            bool foldedAgain = false;
            Sta(() =>
            {
                var c = new ChatControl();
                var lib = new List<SkillInfo> { new SkillInfo { Name = "ve-mep", Title = "Vẽ MEP", Builtin = true, Folder = dir, File = file, Description = "Chuẩn vẽ MEP mẫu" } };
                c.SetSkills(lib, new string[0]);
                var fold = All(c).OfType<Button>().First(b => (b.Content as string) == "⌄");
                fold.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                var view = All(c).OfType<MarkdownView>().Single(v => v.Name == "SkillPreview");
                shown = view.Markdown;
                maxHeight = All(c).OfType<ScrollViewer>().First(s => s.Content == view).MaxHeight;
                All(c).OfType<Button>().First(b => (b.Content as string) == "⌃").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                foldedAgain = !All(c).OfType<MarkdownView>().Any(v => v.Name == "SkillPreview");
            });
            Assert.StartsWith("# Vẽ MEP", shown);           // the frontmatter is not shown
            Assert.Contains("| `E-LIGHT` | Đèn |", shown);
            Assert.Equal(ChatControl.SkillPreviewHeight, maxHeight);
            Assert.True(foldedAgain);
            Assert.Equal(written, File.GetLastWriteTimeUtc(file)); // read only
            Assert.Single(Directory.GetFiles(dir));                // no copy, nothing written
        }
    }
}
