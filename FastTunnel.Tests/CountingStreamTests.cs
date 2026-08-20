using FastTunnel.Core;
using FastTunnel.Core.Utilitys;
using Xunit;

namespace FastTunnel.Tests;

public class CountingStreamTests
{
    [Fact]
    public async Task 同步读写均计数()
    {
        var stats = new TrafficStats();
        var inner = new MemoryStream();
        await using var stream = new CountingStream(inner, stats, "token-a");

        var buffer = new byte[50];
        stream.Write(buffer, 0, 50);
        stream.Position = 0;
        stream.Read(buffer, 0, 50);

        var data = stats.Snapshot(24)["token-a"];
        Assert.Equal(100, data[^1]); // 写 50 + 读 50
    }

    [Fact]
    public async Task Memory异步读写均计数()
    {
        var stats = new TrafficStats();
        var inner = new MemoryStream();
        await using var stream = new CountingStream(inner, stats, "token-a");

        await stream.WriteAsync(new byte[30]);
        inner.Position = 0;
        var buf = new byte[30];
        await stream.ReadAsync(buf);

        var data = stats.Snapshot(24)["token-a"];
        Assert.Equal(60, data[^1]);
    }

    [Fact]
    public async Task byte数组异步重载计数()
    {
        var stats = new TrafficStats();
        var inner = new MemoryStream();
        await using var stream = new CountingStream(inner, stats, "token-a");

        await stream.WriteAsync(new byte[20], 0, 20);
        inner.Position = 0;
        var buf = new byte[20];
        await stream.ReadAsync(buf, 0, 20);

        var data = stats.Snapshot(24)["token-a"];
        Assert.Equal(40, data[^1]);
    }

    [Fact]
    public void CanSeek恒为false()
    {
        var stream = new CountingStream(new MemoryStream(), new TrafficStats(), "token-a");
        Assert.False(stream.CanSeek);
    }

    [Fact]
    public void 零字节写入不计数()
    {
        var stats = new TrafficStats();
        var stream = new CountingStream(new MemoryStream(), stats, "token-a");

        stream.Write([], 0, 0);
        Assert.Empty(stats.Snapshot(24)); // 无流量不产生 Token 桶
    }
}
