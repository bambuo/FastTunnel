// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FastTunnel.Core.Models;

/// <summary>
///     按 Token 的流量统计（进程内存，滑动窗口：最近 24 个小时桶）。
///     服务重启后统计清零。
/// </summary>
public class TrafficStats
{
    public const int BucketCount = 24;
    private static readonly TimeSpan BucketSize = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, TokenTraffic> _traffic = new();

    /// <summary>
    ///     累计指定 Token 的流量（字节）
    /// </summary>
    public void Add(string token, long bytes)
    {
        if (string.IsNullOrEmpty(token) || bytes <= 0) return;
        _traffic.GetOrAdd(token, _ => new TokenTraffic()).Add(bytes);
    }

    /// <summary>
    ///     返回每个有流量的 Token 最近 hours 个小时桶（时间从旧到新）。
    /// </summary>
    public Dictionary<string, long[]> Snapshot(int hours)
    {
        if (hours <= 0 || hours > BucketCount) hours = BucketCount;

        var result = new Dictionary<string, long[]>();
        foreach (var (token, traffic) in _traffic)
        {
            var data = traffic.Snapshot(hours);
            var hasData = false;
            foreach (var v in data)
            {
                if (v > 0) { hasData = true; break; }
            }
            if (hasData)
            {
                result[token] = data;
            }
        }
        return result;
    }

    /// <summary>
    ///     当前 UTC 小时桶的时间标签（"yyyy-MM-dd HH:00"，本地时区由前端转换）
    /// </summary>
    public static DateTime CurrentBucketStart => DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % BucketSize.Ticks));

    private sealed class TokenTraffic
    {
        private readonly object _lock = new();
        private readonly long[] _buckets = new long[BucketCount];
        private DateTime _currentBucket = CurrentBucketStart;

        public void Add(long bytes)
        {
            lock (_lock)
            {
                AdvanceTo(DateTime.UtcNow);
                var index = IndexOf(_currentBucket);
                _buckets[index] += bytes;
            }
        }

        public long[] Snapshot(int hours)
        {
            lock (_lock)
            {
                AdvanceTo(DateTime.UtcNow);
                var end = IndexOf(_currentBucket);
                var result = new long[hours];
                for (var i = 0; i < hours; i++)
                {
                    var idx = (end - hours + 1 + i + BucketCount * 2) % BucketCount;
                    result[i] = _buckets[idx];
                }
                return result;
            }
        }

        private void AdvanceTo(DateTime now)
        {
            var current = CurrentBucketStart;
            if (current <= _currentBucket) return;

            // 跨越的桶清零（最多 24 个）
            var steps = (int)((current - _currentBucket).Ticks / BucketSize.Ticks);
            if (steps >= BucketCount)
            {
                Array.Clear(_buckets, 0, BucketCount);
            }
            else
            {
                for (var i = 1; i <= steps; i++)
                {
                    _buckets[(_currentBucket.AddTicks(i * BucketSize.Ticks).Hour) % BucketCount] = 0;
                }
            }
            _currentBucket = current;
        }

        private static int IndexOf(DateTime bucket) => bucket.Hour % BucketCount;
    }
}
