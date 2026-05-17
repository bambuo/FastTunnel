using FastTunnel.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FastTunnel.Api.Data;

public class FastTunnelDbContext(DbContextOptions<FastTunnelDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<TokenEntity> Tokens => Set<TokenEntity>();
    public DbSet<WebTunnelEntity> WebTunnels => Set<WebTunnelEntity>();
    public DbSet<ForwardTunnelEntity> ForwardTunnels => Set<ForwardTunnelEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TokenEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Value).IsUnique();
            e.Property(x => x.IsDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<WebTunnelEntity>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ForwardTunnelEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RemotePort).IsUnique();
        });

        modelBuilder.Entity<AuditLogEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedAt);
        });
    }
}
