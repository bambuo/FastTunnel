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
}
