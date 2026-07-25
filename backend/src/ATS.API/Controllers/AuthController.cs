using System.Security.Claims;
using ATS.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? ClientUserAgent => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;

    /// <summary>Register a new user. Sends a verification email.</summary>
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command with { IpAddress = ClientIp, UserAgent = ClientUserAgent }, ct);
        return Ok(result);
    }

    /// <summary>Login with email + password. Locks the account for 15 minutes after 5 failed attempts.</summary>
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command with { IpAddress = ClientIp, UserAgent = ClientUserAgent }, ct);
        return Ok(result);
    }

    /// <summary>Exchange a refresh token for a new access token.</summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Revoke a refresh token. Always succeeds even if the token is unknown or already revoked.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>Request a password reset email. Always returns 200 regardless of whether the email exists.</summary>
    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    /// <summary>Reset a password using the token emailed by /forgot-password. Revokes all active sessions.</summary>
    [EnableRateLimiting("auth")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return Ok(new { message = "Password has been reset. Please log in again." });
    }

    /// <summary>Change password while logged in (requires the current password).</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest body, CancellationToken ct)
    {
        await _mediator.Send(new ChangePasswordCommand(CurrentUserId, body.CurrentPassword, body.NewPassword), ct);
        return NoContent();
    }

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    /// <summary>Verify an email address using the token emailed on registration.</summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return Ok(new { message = "Email verified." });
    }

    /// <summary>Resend the verification email for the logged-in user.</summary>
    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification(CancellationToken ct)
    {
        await _mediator.Send(new ResendVerificationEmailCommand(CurrentUserId), ct);
        return NoContent();
    }
}
