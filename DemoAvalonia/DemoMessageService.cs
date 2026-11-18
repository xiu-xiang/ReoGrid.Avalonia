using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;

namespace unvell.ReoGrid.WPFDemo;

/// <summary>示例程序定义的消息窗口；实际调用程序可替换为自己的主题服务。</summary>
public sealed class DemoMessageService : IReoGridMessageService
{
    /// <summary>显示提示。</summary>
    /// <param name="message">内容。</param><param name="title">标题。</param>
    /// <returns>关闭任务。</returns><remarks>实现表格消息接口。</remarks>
    public async Task ShowAsync(string message, string title) => await OpenAsync(message, title, false, false);
    /// <summary>显示确认窗口。</summary>
    /// <param name="message">内容。</param><param name="title">标题。</param>
    /// <returns>是否确认。</returns><remarks>实现表格消息接口。</remarks>
    public async Task<bool> ConfirmAsync(string message, string title) => await OpenAsync(message, title, true, false) != null;
    /// <summary>显示输入窗口。</summary>
    /// <param name="message">内容。</param><param name="title">标题。</param>
    /// <returns>文本或取消的 null。</returns><remarks>实现表格消息接口。</remarks>
    public Task<string> PromptAsync(string message, string title) => OpenAsync(message, title, true, true);
    /// <summary>由宿主统一创建示例窗口。</summary>
    /// <remarks>替代组件原有删除确认与筛选输入窗口。</remarks>
    private static async Task<string> OpenAsync(string message, string title, bool cancel, bool input)
    {
        // 始终使用当前可见主窗口作为 owner；关闭按钮按取消处理。
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null || !owner.IsVisible)
            throw new System.InvalidOperationException("消息窗口需要可见的宿主窗口。");
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        var text = new TextBox();
        if (input) panel.Children.Add(text);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var ok = new Button { Content = "确定", IsDefault = true, MinWidth = 72 };
        var window = new Window { Title = title, Width = 380, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel };
        ok.Click += (_, _) => window.Close(input ? text.Text ?? string.Empty : string.Empty);
        buttons.Children.Add(ok);
        if (cancel)
        {
            var back = new Button { Content = "取消", IsCancel = true, MinWidth = 72 };
            back.Click += (_, _) => window.Close();
            buttons.Children.Add(back);
        }
        panel.Children.Add(buttons);
        return await window.ShowDialog<string>(owner);
    }
}
