using System.Threading.Tasks;

namespace unvell.ReoGrid;

/// <summary>调用程序实现的消息与输入窗口服务；实现负责窗口样式及当前 owner。</summary>
public interface IReoGridMessageService
{
    /// <summary>显示提示或错误。</summary>
    /// <param name="message">消息内容。</param><param name="title">标题。</param>
    /// <returns>窗口关闭任务。</returns>
    /// <remarks>替代组件内部消息窗口。</remarks>
    Task ShowAsync(string message, string title);
    /// <summary>请求确认。</summary>
    /// <param name="message">消息内容。</param><param name="title">标题。</param>
    /// <returns>仅用户确认时为 true。</returns>
    /// <remarks>对应工作表删除确认。</remarks>
    Task<bool> ConfirmAsync(string message, string title);
    /// <summary>请求单项文本输入。</summary>
    /// <param name="message">输入说明。</param><param name="title">标题。</param>
    /// <returns>输入文本；取消为 null。</returns>
    /// <remarks>对应筛选条件输入。</remarks>
    Task<string> PromptAsync(string message, string title);
}
