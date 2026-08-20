// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Runtime.InteropServices;
using FastTunnel.Core.Models;

namespace FastTunnel.Core.Utilitys;

/// <summary>
///     采集客户端运行环境信息（系统、CPU、内存、.NET 版本），登录时上报。
///     纯托管实现：内存统一用 GC.GetGCMemoryInfo()（全平台一致的估算方式）。
/// </summary>
public static class ClientInfoCollector
{
    public static ClientInfo Collect()
    {
        string os;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) os = "Windows";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) os = "macOS";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) os = "Linux";
        else os = "其他";

        return new ClientInfo
        {
            OS = os,
            OSVersion = Environment.OSVersion.VersionString,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            CpuCores = Environment.ProcessorCount,
            AvailableMemoryMB = BytesToMB(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes),
            DotnetVersion = Environment.Version.ToString(),
        };
    }

    private static long BytesToMB(long bytes) => bytes / 1024 / 1024;
}
