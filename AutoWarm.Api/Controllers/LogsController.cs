using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.Logs;
using AutoWarm.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWarm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private readonly IWarmupJobService _service;

    public LogsController(IWarmupJobService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? mailAccountId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var filter = new WarmupLogFilter(mailAccountId, from, to);
        var logs = await _service.GetLogsAsync(userId, filter, cancellationToken);
        return Ok(logs);
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(id!);
    }
}
