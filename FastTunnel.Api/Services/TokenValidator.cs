using FastTunnel.Api.Data;
using FastTunnel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FastTunnel.Api.Services;

/// <summary>
///     校验客户端 Token：必须存在于数据库（管理台创建）、未删除且启用中。
///     使用 IServiceScopeFactory 自建作用域（FastTunnelClientHandler 从根容器解析）。
/// </summary>
public class TokenValidator(IServiceScopeFactory scopeFactory) : ITokenValidator
{
    public async Task<bool> IsValidAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FastTunnelDbContext>();
        return await db.Tokens.AnyAsync(x => x.Value == token && !x.IsDeleted && x.IsEnabled);
    }
}
