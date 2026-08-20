namespace FastTunnel.Api.Utils;

/// <summary>
///     Token 掩码显示工具（管理台统一使用）
/// </summary>
public static class TokenMasker
{
    public static string Mask(string token)
    {
        if (string.IsNullOrEmpty(token)) return "(未设置)";
        return token.Length <= 8 ? token : $"{token[..4]}****{token[^4..]}";
    }
}
