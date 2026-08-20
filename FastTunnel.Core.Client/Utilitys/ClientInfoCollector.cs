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
///     内存采集跨平台尽力而为，失败返回 0。
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsMemory();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemory();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacMemory();
            }
        }
        catch
        {
            // 内存采集失败时返回 0，不影响其他信息
        }
        return (0, 0);
    }

    private static (long, long) GetMacMemory()
    {
        var size = (nuint)sizeof(ulong);
        if (sysctlbyname("hw.memsize", out var total, ref size, IntPtr.Zero, 0) == 0)
        {
            return (BytesToMB(total), 0);
        }
        return (0, 0);
    }

    private static (long, long) GetWindowsMemory()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status)) return (0, 0);
        return (BytesToMB(status.ullTotalPhys), BytesToMB(status.ullAvailPhys));
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

    private static long BytesToMB(ulong bytes) => (long)(bytes / 1024 / 1024);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("libc", SetLastError = true)]
    private static extern int sysctlbyname(string name, out ulong value, ref nuint size, IntPtr newp, nuint newlen);
}
