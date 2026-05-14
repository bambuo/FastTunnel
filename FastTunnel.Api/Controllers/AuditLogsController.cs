using FastTunnel.Api.Data;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastTunnel.Api.Controllers;

[Authorize]
[Route("api/audit-logs")]
[ApiController]
public class AuditLogsController(FastTunnelDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null,
        [FromQuery] string? entity = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(x => x.Entity == entity);
        if (DateTime.TryParse(startDate, out var sd))
            query = query.Where(x => x.CreatedAt >= sd);
        if (DateTime.TryParse(endDate, out var ed))
            query = query.Where(x => x.CreatedAt <= ed.AddDays(1));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Action,
                x.Entity,
                x.Detail,
                x.Operator,
                createdAt = x.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            })
            .ToListAsync();

        return new JsonResult(new ApiResponse
        {
            Success = true,
            Data = new { items, total, page, pageSize },
        });
    }
}
