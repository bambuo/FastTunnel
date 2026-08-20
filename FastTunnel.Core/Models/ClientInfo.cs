// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

namespace FastTunnel.Core.Models;

/// <summary>
///     客户端运行环境信息（登录时上报，用于管理台展示）
/// </summary>
public class ClientInfo
{
    /// <summary>操作系统名称：Windows / macOS / Linux / 其他</summary>
    public string OS { get; set; } = string.Empty;

    /// <summary>操作系统版本（尽力而为，可能为空）</summary>
    public string OSVersion { get; set; } = string.Empty;

    /// <summary>进程架构：x64 / arm64 / x86 等</summary>
    public string Architecture { get; set; } = string.Empty;

    /// <summary>逻辑处理器数量</summary>
    public int CpuCores { get; set; }

    /// <summary>总内存（MB），未知为 0</summary>
    public long TotalMemoryMB { get; set; }

    /// <summary>可用内存（MB），未知为 0</summary>
    public long AvailableMemoryMB { get; set; }

    /// <summary>.NET 运行时版本</summary>
    public string DotnetVersion { get; set; } = string.Empty;
}
