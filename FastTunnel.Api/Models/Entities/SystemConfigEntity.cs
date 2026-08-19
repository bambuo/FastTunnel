namespace FastTunnel.Api.Models.Entities;

/// <summary>
///     系统配置项（FastTunnel 段，键相对 FastTunnel 段，如 "EnableForward"、"Api:JWT:Expires"）。
/// </summary>
public class SystemConfigEntity
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
