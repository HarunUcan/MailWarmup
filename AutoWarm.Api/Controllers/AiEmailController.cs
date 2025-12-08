using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWarm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai/email")]
public class AiEmailController : ControllerBase
{
    private readonly IEmailOptimizationService _emailOptimizationService;

    public AiEmailController(IEmailOptimizationService emailOptimizationService)
    {
        _emailOptimizationService = emailOptimizationService;
    }

    [HttpPost("optimize")]
    public async Task<IActionResult> Optimize([FromBody] AiEmailOptimizeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) && string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest("Subject ve Body alanlarından en az biri dolu olmalıdır.");
        }

        var result = await _emailOptimizationService.OptimizeAsync(request, cancellationToken);
        return Ok(result);
    }
}
