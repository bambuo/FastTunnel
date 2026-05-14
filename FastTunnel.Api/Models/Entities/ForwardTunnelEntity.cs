namespace FastTunnel.Api.Models.Entities;

public class ForwardTunnelEntity
{
    public int Id { get; set; }
    public int RemotePort { get; set; }
    public string LocalIp { get; set; } = string.Empty;
    public int LocalPort { get; set; }
    public string Protocol { get; set; } = "TCP";
    public int ClientId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
