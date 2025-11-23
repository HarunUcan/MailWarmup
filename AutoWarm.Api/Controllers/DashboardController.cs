using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWarm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IWarmupJobService _service;

    public DashboardController(IWarmupJobService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var summary = await _service.GetDashboardSummaryAsync(userId, cancellationToken);
        return Ok(summary);
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(id!);
    }
}
