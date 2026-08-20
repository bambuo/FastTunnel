using FastTunnel.Api.Data;
using FastTunnel.Api.Resources;
using FastTunnel.Core.Client;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FastTunnel.Api.Controllers;

public class ClientsController(FastTunnelDbContext db, FastTunnelServer ftServer, IStringLocalizer<ApiMessages> localizer) : BaseController
{
    private readonly IStringLocalizer<ApiMessages> _localizer = localizer;
    [HttpGet]
    public IActionResult Index()
    {
        var clients = ftServer.Clients.Select(x => new
        {
            id = x.RemoteIpAddress.GetHashCode() ^ x.StartTime.Ticks,
            name = $"Client@{x.RemoteIpAddress}",
            ip = x.RemoteIpAddress.ToString(),
            token = x.Token,
            tokenPreview = MaskToken(x.Token),
            isOnline = true,
            lastSeen = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            clientInfo = x.ClientInfo,
        }).ToList();

        ApiResponse.Data = clients;
        ApiResponse.Success = true;
        return new JsonResult(ApiResponse);
    }

    private static string MaskToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return "(未设置)";
        return token.Length <= 8 ? token : $"{token[..4]}****{token[^4..]}";
    }

    [HttpGet("online/count")]
    public ApiResponse OnlineCount()
    {
        ApiResponse.Data = ftServer.ConnectedClientCount;
        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpGet("{id}/tunnels")]
    public IActionResult Tunnels(int id)
    {
        var client = ftServer.Clients.FirstOrDefault(c =>
            (c.RemoteIpAddress.GetHashCode() ^ c.StartTime.Ticks) == id);

        if (client == null)
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Client.NotFound"];
            return new JsonResult(ApiResponse);
        }

        ApiResponse.Data = new
        {
            webs = client.WebInfos.Select(w => new
            {
                id = w.WebConfig.GetHashCode(),
                subDomain = w.WebConfig.SubDomain,
                isEnabled = true,
            }),
            forwards = client.ForwardInfos.Select(f => new
            {
                id = f.SSHConfig.GetHashCode(),
                remotePort = f.SSHConfig.RemotePort,
                isEnabled = true,
            }),
        };
        ApiResponse.Success = true;
        return new JsonResult(ApiResponse);
    }
}
