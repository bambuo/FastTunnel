namespace FastTunnel.Api.Services;

public static class AuditService
{
    public static async Task LogAsync(
        FastTunnel.Api.Data.FastTunnelDbContext db,
        string action, string entity, string detail, string operatorName,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new Models.Entities.AuditLogEntity
        {
            Action = action,
            Entity = entity,
            Detail = detail,
            Operator = operatorName,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
