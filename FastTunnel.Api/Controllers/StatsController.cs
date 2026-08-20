using FastTunnel.Api.Data;
using FastTunnel.Core.Client;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastTunnel.Api.Controllers;

public class StatsController(FastTunnelDbContext db, FastTunnelServer ftServer) : BaseController
{
    [HttpGet("overview")]
    public async Task<ApiResponse> Overview()
    {
        var activeWebs = await db.WebTunnels.CountAsync(x => x.IsEnabled);
        var activeForwards = await db.ForwardTunnels.CountAsync(x => x.IsEnabled);
        var totalTokens = await db.Tokens.CountAsync();

        ApiResponse.Data = new
        {
            onlineClientCount = ftServer.ConnectedClientCount,
            activeWebTunnelCount = activeWebs,
            activeForwardTunnelCount = activeForwards,
            totalTokenCount = totalTokens,
        };
        ApiResponse.Success = true;
        return ApiResponse;
    }

    /// <summary>
    ///     按 Token 的流量趋势（近 N 小时，小时粒度，内存统计）
    /// </summary>
    [HttpGet("traffic")]
    public ApiResponse Traffic([FromQuery] int hours = 24)
    {
        if (hours <= 0 || hours > 24) hours = 24;

        var snapshot = ftServer.Traffic.Snapshot(hours);
        var hourLabels = new string[hours];
        var end = FastTunnel.Core.Models.TrafficStats.CurrentBucketStart;
        for (var i = hours - 1; i >= 0; i--)
        {
            hourLabels[i] = end.AddHours(-(hours - 1 - i)).ToString("yyyy-MM-ddTHH:00:00Z");
        }

        ApiResponse.Data = new
        {
            hours = hourLabels,
            series = snapshot.Select(kv => new
            {
                token = kv.Key,
                label = MaskToken(kv.Key),
                data = kv.Value,
            }),
        };
        ApiResponse.Success = true;
        return ApiResponse;
    }

    private static string MaskToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return "(未设置)";
        return token.Length <= 8 ? token : $"{token[..4]}****{token[^4..]}";
    }
}
