using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ClaudeCAD.Plugin.UI;
using Xunit;

namespace ClaudeCAD.UI.Tests
{
    /// <summary>v0.9.8: "Thêm skill từ Word" — the dialog builds, suggests the name and reports what was dropped.</summary>
    public class SkillInfoDialogTests
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

        [Fact]
        public void BuildsWithTheSuggestedNameAndTheDroppedParts()
        {
            string name = null, desc = null, text = null;
            Sta(() =>
            {
                var d = new SkillInfoDialog(@"C:\tai lieu\Chuẩn MEP.docx", "chuan-mep", "2 ảnh / hình vẽ");
                var boxes = ((StackPanel)d.Content).Children.OfType<TextBox>().ToList();
                Assert.Equal(2, boxes.Count);
                boxes[1].Text = "Chuẩn vẽ MEP của công ty A\n - layer E-*, P-*.";
                name = d.SkillName;
                desc = d.Description;
                text = string.Join("\n", ((StackPanel)d.Content).Children.OfType<TextBlock>().Select(t => t.Text));
                d.Close();
            });
            Assert.Equal("chuan-mep", name);
            Assert.Equal("Chuẩn vẽ MEP của công ty A - layer E-*, P-*.", desc);
            Assert.Contains("Chuẩn MEP.docx", text);
            Assert.Contains("2 ảnh / hình vẽ", text);
        }

        [Fact]
        public void TheEditedSkillPopupPrefillsNameAndDescription()
        {
            string name = null, desc = null, text = null;
            Sta(() =>
            {
                var d = SkillInfoDialog.ForEditedSkill("ve-mep", "Default", "ve-mep-sua", "Chuẩn vẽ MEP mẫu - điện nước.", "Lưu ý: Bảng layer chuẩn, dòng 9 …", (n, x) => null);
                name = d.SkillName;
                desc = d.Description;
                text = string.Join("\n", ((StackPanel)d.Content).Children.OfType<TextBlock>().Select(t => t.Text));
                var buttons = ((StackPanel)((StackPanel)d.Content).Children[((StackPanel)d.Content).Children.Count - 1]).Children.OfType<Button>().Select(b => (string)b.Content).ToList();
                Assert.Equal(new[] { "Tạo skill", "Để sau" }, buttons);
                d.Close();
            });
            Assert.Equal("ve-mep-sua", name);
            Assert.Equal("Chuẩn vẽ MEP mẫu - điện nước.", desc);
            Assert.Contains("Bản gốc được giữ nguyên", text);
            Assert.Contains("Bảng layer chuẩn", text);
        }
    }
}
