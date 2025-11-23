using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.WarmupProfiles;
using AutoWarm.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWarm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/warmup-profiles")]
public class WarmupProfilesController : ControllerBase
{
    private readonly IWarmupProfileService _profiles;

    public WarmupProfilesController(IWarmupProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet("{mailAccountId:guid}")]
    public async Task<IActionResult> GetByMailAccount(Guid mailAccountId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _profiles.GetByMailAccountAsync(userId, mailAccountId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarmupProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _profiles.CreateAsync(userId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarmupProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _profiles.UpdateAsync(userId, id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _profiles.DeleteAsync(userId, id, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(id!);
    }
}
