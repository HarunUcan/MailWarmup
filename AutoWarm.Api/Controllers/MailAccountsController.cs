using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.MailAccounts;
using AutoWarm.Application.DTOs.Logs;
using AutoWarm.Application.Interfaces;
using AutoWarm.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AutoWarm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mail-accounts")]
public class MailAccountsController : ControllerBase
{
    private readonly IMailAccountService _mailAccountService;
    private readonly GmailOAuthOptions _gmailOptions;

    public MailAccountsController(IMailAccountService mailAccountService, IOptions<GmailOAuthOptions> gmailOptions)
    {
        _mailAccountService = mailAccountService;
        _gmailOptions = gmailOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var accounts = await _mailAccountService.GetMailAccountsAsync(userId, cancellationToken);
        return Ok(accounts);
    }

    [HttpPost("gmail/start-auth")]
    public async Task<IActionResult> StartGmailAuth(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var response = await _mailAccountService.StartGmailAuthAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("gmail/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GmailCallback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        // state format: {userId}:{random}
        if (string.IsNullOrWhiteSpace(state) || !state.Contains(':'))
        {
            return BadRequest("Missing userId");
        }

        var userIdPart = state.Split(':')[0];
        if (!Guid.TryParse(userIdPart, out var userId))
        {
            return BadRequest("Missing userId");
        }

        var account = await _mailAccountService.CompleteGmailAuthAsync(userId, code, state, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_gmailOptions.FrontendRedirectUri))
        {
            var redirectUri = $"{_gmailOptions.FrontendRedirectUri.TrimEnd('/')}/accounts?provider=gmail&status=success";
            return Redirect(redirectUri);
        }

        return Ok(account);
    }

    [HttpPost("custom")]
    public async Task<IActionResult> CreateCustom([FromBody] CreateCustomMailAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var account = await _mailAccountService.CreateCustomAsync(userId, request, cancellationToken);
        return Ok(account);
    }

    [HttpPost("{id:guid}/send-test")]
    public async Task<IActionResult> SendTest(Guid id, [FromBody] SendTestMailRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var messageId = await _mailAccountService.SendTestAsync(userId, id, request, cancellationToken);
        return Ok(new { messageId });
    }

    [HttpGet("{id:guid}/fetch")]
    public async Task<IActionResult> Fetch(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var logs = await _mailAccountService.FetchRecentAsync(userId, id, cancellationToken);
        return Ok(logs);
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(id!);
    }
}
