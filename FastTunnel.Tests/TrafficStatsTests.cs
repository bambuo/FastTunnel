using FastTunnel.Core;
using Xunit;

namespace FastTunnel.Tests;

public class TrafficStatsTests
{
    private static readonly DateTime Start = new(2026, 8, 20, 10, 30, 0, DateTimeKind.Utc);

    private static (TrafficStats stats, FakeTimeProvider clock) Create()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(Start));
        return (new TrafficStats(clock), clock);
    }

    [Fact]
    public void Add_累加到当前小时桶()
    {
        var (stats, _) = Create();
        stats.Add("token-a", 100);
        stats.Add("token-a", 200);

        var snapshot = stats.Snapshot(24);
        var data = Assert.Single(snapshot);
        Assert.Equal("token-a", data.Key);
        Assert.Equal(300, data.Value[^1]); // 最后一桶 = 当前小时
        Assert.All(data.Value[..^1], v => Assert.Equal(0, v));
    }

    [Fact]
    public void Snapshot_桶顺序从旧到新()
    {
        var (stats, clock) = Create();
        stats.Add("token-a", 100);
        clock.Advance(TimeSpan.FromHours(2));
        stats.Add("token-a", 500);

        var data = stats.Snapshot(24)[ "token-a"];
        Assert.Equal(100, data[^3]); // 10 点桶
        Assert.Equal(500, data[^1]); // 12 点桶
        Assert.Equal(0, data[^2]);   // 11 点桶无数据
    }

    [Fact]
    public void 跨小时_旧桶保留新桶累计()
    {
        var (stats, clock) = Create();
        stats.Add("token-a", 100); // 10 点桶
        clock.Advance(TimeSpan.FromHours(1));
        stats.Add("token-a", 200); // 11 点桶

        var data = stats.Snapshot(24)["token-a"];
        Assert.Equal(200, data[^1]); // 11 点
        Assert.Equal(100, data[^2]); // 10 点历史保留
    }

    [Fact]
    public void 跨天_23点到0点滚动()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(new DateTime(2026, 8, 19, 23, 50, 0, DateTimeKind.Utc)));
        var stats = new TrafficStats(clock);
        stats.Add("token-a", 100); // 23 点桶
        clock.Advance(TimeSpan.FromMinutes(20)); // 进入 0 点
        stats.Add("token-a", 300); // 0 点桶

        var data = stats.Snapshot(24)["token-a"];
        Assert.Equal(300, data[^1]); // 0 点（最新）
        Assert.Equal(100, data[^2]); // 23 点历史保留
    }

    [Fact]
    public void 多Token统计隔离()
    {
        var (stats, _) = Create();
        stats.Add("token-a", 100);
        stats.Add("token-b", 200);

        var snapshot = stats.Snapshot(24);
        Assert.Equal(2, snapshot.Count);
        Assert.Equal(100, snapshot["token-a"][^1]);
        Assert.Equal(200, snapshot["token-b"][^1]);
    }

    [Fact]
    public void 无流量Token不出现()
    {
        var (stats, _) = Create();
        stats.Add("token-a", 100);
        stats.Add("token-b", 0); // 0 字节不产生桶

        var snapshot = stats.Snapshot(24);
        Assert.Single(snapshot);
        Assert.True(snapshot.ContainsKey("token-a"));
    }

    [Fact]
    public void Snapshot_请求小时数截断()
    {
        var (stats, clock) = Create();
        stats.Add("token-a", 100); // 10 点桶
        clock.Advance(TimeSpan.FromHours(2)); // 12 点

        var data = stats.Snapshot(3)["token-a"];
        Assert.Equal(3, data.Length);
        Assert.Equal(100, data[0]); // 10 点桶仍在最近 3 桶（10,11,12）内
        Assert.Equal(0, data[2]);   // 12 点桶无数据
    }

    [Fact]
    public void Snapshot_超出窗口的数据被截断()
    {
        var (stats, clock) = Create();
        stats.Add("token-a", 100); // 10 点桶
        clock.Advance(TimeSpan.FromHours(5)); // 15 点

        // 最近 3 桶（13,14,15 点）内无流量，token 被过滤（图表不显示空线）
        Assert.Empty(stats.Snapshot(3));
    }

    [Fact]
    public void GetHourLabels_数量与顺序()
    {
        var (stats, _) = Create();
        var labels = stats.GetHourLabels(24);

        Assert.Equal(24, labels.Length);
        Assert.Equal(new DateTime(2026, 8, 19, 11, 0, 0, DateTimeKind.Utc), labels[0]);
        Assert.Equal(new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc), labels[^1]);
        for (var i = 1; i < labels.Length; i++)
        {
            Assert.Equal(labels[i - 1].AddHours(1), labels[i]);
        }
    }
}
