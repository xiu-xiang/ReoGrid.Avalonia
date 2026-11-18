using System;

namespace unvell.ReoGrid;

/// <summary>由调用程序在创建表格前初始化消息服务，组件不创建默认窗口。</summary>
public static class ReoGridMessages
{
    private static IReoGridMessageService service;
    /// <summary>统一消息服务；未初始化时明确报错，避免误确认。</summary>
    public static IReoGridMessageService Service
    {
        get => service ?? throw new InvalidOperationException("请由调用程序初始化 ReoGridMessages.Service。");
        set => service = value ?? throw new ArgumentNullException(nameof(value));
    }
}
