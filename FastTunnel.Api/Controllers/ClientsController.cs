using FastTunnel.Api.Data;
using FastTunnel.Core.Client;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastTunnel.Api.Controllers;

public class ClientsController(FastTunnelDbContext db, FastTunnelServer ftServer) : BaseController
{
    [HttpGet]
    public IActionResult Index()
    {
        var clients = ftServer.Clients.Select(x => new
        {
            id = x.RemoteIpAddress.GetHashCode() ^ x.StartTime.Ticks,
            name = $"Client@{x.RemoteIpAddress}",
            tokenPreview = "****",
            isOnline = true,
            lastSeen = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        }).ToList();

        ApiResponse.Data = clients;
        ApiResponse.Success = true;
        return new JsonResult(ApiResponse);
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
            ApiResponse.Message = "客户端不存在";
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
