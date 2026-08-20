// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.IO;
using System.Runtime.InteropServices;
using FastTunnel.Core.Models;

namespace FastTunnel.Core.Utilitys;

/// <summary>
///     采集客户端运行环境信息（系统、CPU、内存、.NET 版本），登录时上报。
///     纯托管实现：Linux 读 /proc/meminfo（精确总量/可用量），
///     Windows/macOS 用 GC.GetGCMemoryInfo()（可用量近似值 + 内存压力百分比）。
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

        var (total, available) = GetMemory();

        return new ClientInfo
        {
            OS = os,
            OSVersion = Environment.OSVersion.VersionString,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            CpuCores = Environment.ProcessorCount,
            TotalMemoryMB = total,
            AvailableMemoryMB = available,
            DotnetVersion = Environment.Version.ToString(),
        };
    }

    private static (long totalMB, long availableMB) GetMemory()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemory();
            }
        }
        catch
        {
            // 内存采集失败时返回 0，不影响其他信息
        }

        // Windows / macOS / 其他：无纯托管的物理内存总量 API，
        // 可用量用 GC 估算值（近似）
        return (0, BytesToMB(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes));
    }

    private static (long, long) GetLinuxMemory()
    {
        var total = 0L;
        var available = 0L;
        foreach (var line in File.ReadLines("/proc/meminfo"))
        {
            if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
            {
                total = ParseKb(line);
            }
            else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
            {
                available = ParseKb(line);
            }
        }
        return (total, available);
    }

    private static long ParseKb(string line)
    {
        // 形如 "MemTotal:       16384000 kB"
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], out var kb) ? kb / 1024 : 0;
    }

    private static long BytesToMB(long bytes) => bytes / 1024 / 1024;
}
