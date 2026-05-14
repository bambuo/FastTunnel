namespace FastTunnel.Api.Models.Entities;

public class WebTunnelEntity
{
    public int Id { get; set; }
    public string SubDomain { get; set; } = string.Empty;
    public string LocalIp { get; set; } = string.Empty;
    public int LocalPort { get; set; }
    public string? WwwsJson { get; set; }
    public int ClientId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
