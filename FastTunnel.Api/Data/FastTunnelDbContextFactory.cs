using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FastTunnel.Api.Data;

/// <summary>
///     EF 设计时工厂（dotnet ef migrations 使用）。
/// </summary>
public class FastTunnelDbContextFactory : IDesignTimeDbContextFactory<FastTunnelDbContext>
{
    public FastTunnelDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FastTunnelDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new FastTunnelDbContext(options);
    }
}
