namespace FastTunnel.Api.Models.Entities;

public class AuditLogEntity
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
