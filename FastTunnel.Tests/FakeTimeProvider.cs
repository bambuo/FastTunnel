namespace FastTunnel.Tests;

/// <summary>
///     可控时钟（测试用）：默认 UTC 时间可推进
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now += delta;
}
