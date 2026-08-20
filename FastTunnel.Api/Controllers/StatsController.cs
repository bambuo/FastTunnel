using FastTunnel.Api.Data;
using FastTunnel.Api.Utils;
using FastTunnel.Core;
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
        if (hours <= 0 || hours > TrafficStats.BucketCount) hours = TrafficStats.BucketCount;

        var snapshot = ftServer.Traffic.Snapshot(hours);
        var hourLabels = ftServer.Traffic.GetHourLabels(hours);

        ApiResponse.Data = new
        {
            hours = hourLabels.Select(h => h.ToString("yyyy-MM-ddTHH:00:00Z")),
            series = snapshot.Select(kv => new
            {
                token = kv.Key,
                label = TokenMasker.Mask(kv.Key),
                data = kv.Value,
            }),
        };
        ApiResponse.Success = true;
        return ApiResponse;
    }
}
