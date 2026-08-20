// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace FastTunnel.Core;

/// <summary>
///     按 Token 的流量统计（进程内存，滑动窗口：最近 24 个小时桶）。
///     服务重启后统计清零。小时桶写入使用 Interlocked（无锁），
///     跨小时翻转清零与写入之间存在可容忍的边界竞态。
/// </summary>
public class TrafficStats
{
    public const int BucketCount = 24;
    private static readonly TimeSpan BucketSize = TimeSpan.FromHours(1);

    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, TokenTraffic> _traffic = new();

    public TrafficStats(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    ///     累计指定 Token 的流量（字节）
    /// </summary>
    public void Add(string token, long bytes)
    {
        if (string.IsNullOrEmpty(token) || bytes <= 0) return;
        _traffic.GetOrAdd(token, _ => new TokenTraffic(_timeProvider)).Add(bytes);
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
    ///     最近 hours 个小时桶的起点时间（UTC，从旧到新）
    /// </summary>
    public DateTime[] GetHourLabels(int hours)
    {
        if (hours <= 0 || hours > BucketCount) hours = BucketCount;

        var end = FloorHour(_timeProvider.GetUtcNow().UtcDateTime);
        var labels = new DateTime[hours];
        for (var i = 0; i < hours; i++)
        {
            labels[i] = end.AddHours(-(hours - 1 - i));
        }
        return labels;
    }

    private static DateTime FloorHour(DateTime utc) => utc.AddTicks(-(utc.Ticks % BucketSize.Ticks));

    private sealed class TokenTraffic
    {
        private readonly TimeProvider _timeProvider;
        private readonly long[] _buckets = new long[BucketCount];
        private volatile int _currentIndex;

        public TokenTraffic(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _currentIndex = FloorHour(timeProvider.GetUtcNow().UtcDateTime).Hour % BucketCount;
        }

        public void Add(long bytes)
        {
            var index = FloorHour(_timeProvider.GetUtcNow().UtcDateTime).Hour % BucketCount;
            if (index != _currentIndex)
            {
                AdvanceTo(index);
            }
            Interlocked.Add(ref _buckets[index], bytes);
        }

        public long[] Snapshot(int hours)
        {
            // 基于当前时间定位窗口（无流量时 _currentIndex 可能落后，不能用作窗口锚点）
            var end = FloorHour(_timeProvider.GetUtcNow().UtcDateTime).Hour % BucketCount;
            var result = new long[hours];
            for (var i = 0; i < hours; i++)
            {
                var idx = (end - hours + 1 + i + BucketCount * 2) % BucketCount;
                result[i] = Interlocked.Read(ref _buckets[idx]);
            }
            return result;
        }

        /// <summary>
        ///     推进到新小时桶：清零经过的桶（不含旧桶本身——旧小时数据保留为历史；含新桶——可能有上一天同小时的残留）。
        ///     与并发的 Add 存在边界竞态（可能少计一次），可接受。
        /// </summary>
        private void AdvanceTo(int index)
        {
            var steps = (index - _currentIndex + BucketCount) % BucketCount;
            if (steps == 0) return;

            // 清 (旧桶+1) .. 新桶 之间的 steps 个桶
            for (var i = 1; i <= steps; i++)
            {
                Interlocked.Exchange(ref _buckets[(_currentIndex + i) % BucketCount], 0);
            }
            _currentIndex = index;
        }
    }
}
