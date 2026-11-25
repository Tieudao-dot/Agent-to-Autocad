using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClaudeCAD.Plugin.Core;

namespace ClaudeCAD.Plugin.UI
{
    /// <summary>
    /// Name + description for a new skill (v0.9.8):
    ///  * "Thêm skill từ Word" — a Word file without its own name / description;
    ///  * "Tạo skill mới từ skill gốc" — a Default / Admin skill edited in a working copy and saved.
    /// The description (when to use the skill) is what Claude reads to load it.
    /// </summary>
    public sealed class SkillInfoDialog : Window
    {
        private readonly TextBox _name, _desc;
        private readonly TextBlock _error;
        private readonly Func<string, string, string> _check;

        public string SkillName => SkillStore.Slugify(_name.Text);
        public string Description => SkillStore.OneLine(_desc.Text);

        /// <summary>"Thêm skill từ Word".</summary>
        public SkillInfoDialog(string file, string suggestedName, string dropped)
            : this("ClaudeCAD — Thêm skill từ Word",
                   $"File \"{System.IO.Path.GetFileName(file)}\" chưa có phần đầu (name / description). Đặt tên và mô tả cho skill:",
                   suggestedName, "", string.IsNullOrEmpty(dropped) ? null : "Bỏ khi chuyển: " + dropped, "Thêm skill", "Hủy",
                   SkillStore.CheckNewSkillInfo)
        { }

        /// <summary>"Tạo skill mới từ skill gốc": the original stays as it is; the edit becomes a new You skill.</summary>
        public static SkillInfoDialog ForEditedSkill(string original, string label, string suggestedName, string description,
            string notice, Func<string, string, string> check) =>
            new SkillInfoDialog("ClaudeCAD — Tạo skill mới từ skill gốc",
                $"Cậu vừa sửa \"{original}\" (skill {label}). Bản gốc được giữ nguyên; phần sửa sẽ thành một skill mới của cậu (nhãn You). " +
                "Đặt tên và mô tả cho skill mới:",
                suggestedName, description, notice, "Tạo skill", "Để sau", check);

        private SkillInfoDialog(string title, string intro, string name, string description, string notice,
            string okText, string cancelText, Func<string, string, string> check)
        {
            _check = check ?? SkillStore.CheckNewSkillInfo;
            Title = title;
            Width = 480;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            ShowInTaskbar = false;

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(new TextBlock { Text = intro, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
            root.Children.Add(new TextBlock { Text = "Tên skill (chữ thường không dấu, số, gạch nối)", FontWeight = FontWeights.SemiBold });
            _name = new TextBox { Text = name ?? "", Margin = new Thickness(0, 2, 0, 10), Name = "SkillName" };
            root.Children.Add(_name);
            root.Children.Add(new TextBlock { Text = "Mô tả: khi nào dùng skill này + từ khóa (20–1024 ký tự)", FontWeight = FontWeights.SemiBold });
            root.Children.Add(new TextBlock
            {
                Text = "Ví dụ: Chuẩn vẽ MEP của công ty A - layer E-*, P-*. Dùng khi vẽ điện, chiếu sáng, cấp thoát nước cho công ty A.",
                FontSize = 11, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap,
            });
            _desc = new TextBox { Text = description ?? "", AcceptsReturn = false, TextWrapping = TextWrapping.Wrap, Height = 64, Margin = new Thickness(0, 2, 0, 6), Name = "SkillDescription" };
            root.Children.Add(_desc);
            if (!string.IsNullOrEmpty(notice))
                root.Children.Add(new TextBlock { Text = notice, FontSize = 11, Foreground = Brushes.DarkGoldenrod, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            _error = new TextBlock { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 0, 0, 6) };
            root.Children.Add(_error);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = okText, IsDefault = true, MinWidth = 90, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(8, 2, 8, 2) };
            var cancel = new Button { Content = cancelText, IsCancel = true, MinWidth = 70, Padding = new Thickness(8, 2, 8, 2) };
            ok.Click += (s, e) =>
            {
                var why = _check(_name.Text, _desc.Text);
                if (why != null) { _error.Text = why; _error.Visibility = Visibility.Visible; return; }
                DialogResult = true;
            };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            root.Children.Add(buttons);
            Content = root;
            Loaded += (s, e) => { if (string.IsNullOrEmpty(_desc.Text)) _desc.Focus(); else _name.Focus(); };
        }
    }
}
