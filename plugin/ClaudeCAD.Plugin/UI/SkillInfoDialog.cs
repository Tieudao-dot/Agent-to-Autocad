using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClaudeCAD.Plugin.Core;

namespace ClaudeCAD.Plugin.UI
{
    /// <summary>
    /// "Thêm skill từ Word" (v0.9.8): a Word file without its own name / description gets them here — the name is
    /// suggested from the file, the description (when to use the skill) is what Claude reads to load it.
    /// </summary>
    public sealed class SkillInfoDialog : Window
    {
        private readonly TextBox _name, _desc;
        private readonly TextBlock _error;

        public string SkillName => SkillStore.Slugify(_name.Text);
        public string Description => SkillStore.OneLine(_desc.Text);

        public SkillInfoDialog(string file, string suggestedName, string dropped)
        {
            Title = "ClaudeCAD — Thêm skill từ Word";
            Width = 480;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = false;

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(new TextBlock
            {
                Text = $"File \"{System.IO.Path.GetFileName(file)}\" chưa có phần đầu (name / description). Đặt tên và mô tả cho skill:",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
            });
            root.Children.Add(new TextBlock { Text = "Tên skill (chữ thường không dấu, số, gạch nối)", FontWeight = FontWeights.SemiBold });
            _name = new TextBox { Text = suggestedName ?? "", Margin = new Thickness(0, 2, 0, 10), Name = "SkillName" };
            root.Children.Add(_name);
            root.Children.Add(new TextBlock { Text = "Mô tả: khi nào dùng skill này + từ khóa (20–1024 ký tự)", FontWeight = FontWeights.SemiBold });
            root.Children.Add(new TextBlock
            {
                Text = "Ví dụ: Chuẩn vẽ MEP của công ty A - layer E-*, P-*. Dùng khi vẽ điện, chiếu sáng, cấp thoát nước cho công ty A.",
                FontSize = 11, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap,
            });
            _desc = new TextBox { AcceptsReturn = false, TextWrapping = TextWrapping.Wrap, Height = 64, Margin = new Thickness(0, 2, 0, 6), Name = "SkillDescription" };
            root.Children.Add(_desc);
            if (!string.IsNullOrEmpty(dropped))
                root.Children.Add(new TextBlock { Text = "Bỏ khi chuyển: " + dropped, FontSize = 11, Foreground = Brushes.DarkGoldenrod, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            _error = new TextBlock { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 0, 0, 6) };
            root.Children.Add(_error);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "Thêm skill", IsDefault = true, MinWidth = 90, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(8, 2, 8, 2) };
            var cancel = new Button { Content = "Hủy", IsCancel = true, MinWidth = 70, Padding = new Thickness(8, 2, 8, 2) };
            ok.Click += (s, e) =>
            {
                var why = SkillStore.CheckNewSkillInfo(_name.Text, _desc.Text);
                if (why != null) { _error.Text = why; _error.Visibility = Visibility.Visible; return; }
                DialogResult = true;
            };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            root.Children.Add(buttons);
            Content = root;
            Loaded += (s, e) => { _desc.Focus(); };
        }
    }
}
