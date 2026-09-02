using LibrarySystem.API.Extensions;
using LibrarySystem.Business.DTOs.Auth;
using LibrarySystem.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.Controllers;

/// <summary>
/// Issues JWT bearer tokens for registered users and admins.
/// Architectural note: the original documentation requires JWT authentication for
/// every endpoint but defines no token-issuing endpoint; POST /api/auth/login is
/// therefore added so the documented <c>Authorization: Bearer &lt;token&gt;</c>
/// contract is usable end-to-end.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    /// <summary>
    /// Authenticates with email/password and returns a signed JWT.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The token payload on success; 401/403 on invalid credentials or deactivated accounts.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Registers a new user account, persists it, and returns a signed JWT so
    /// the caller is signed in immediately.
    /// </summary>
    /// <param name="request">Registration details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The token payload on success; 400/409 on invalid input or duplicate email.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Created(string.Empty, result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Emails a single-use password reset link. Responds identically whether
    /// or not the email is registered, to prevent account enumeration.
    /// </summary>
    /// <param name="request">The email requesting a reset.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>200 with a generic confirmation; 400 on invalid input.</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(new { message = "If an account exists for this email, a reset link has been sent." })
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Completes a password reset using the emailed single-use token.
    /// </summary>
    /// <param name="request">Email, raw token and the new password.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>200 on success; 400/422 for invalid or expired tokens.</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(new { message = "Password updated. You can now sign in with your new password." })
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// One-time bootstrap: creates the very first administrator account while
    /// no admin exists in the database. Returns 409 afterwards — additional
    /// admins are created through <c>POST /api/admin/users</c>.
    /// </summary>
    /// <param name="request">Admin account details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>201 with the issued token pair; 400/409 on invalid input or an existing admin.</returns>
    [HttpPost("create-admin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.CreateAdminAsync(request, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Created(string.Empty, result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a fresh access/refresh token pair.
    /// The presented token is rotated (revoked) so it cannot be replayed.
    /// </summary>
    /// <param name="request">The refresh token to exchange.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>200 with the new token pair; 401 for invalid/expired/revoked tokens.</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Revokes the caller's refresh token (logout). Idempotent.
    /// </summary>
    /// <param name="request">The refresh token to revoke.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>204 on success.</returns>
    [HttpPost("revoke-refresh-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RevokeRefreshTokenAsync(request.RefreshToken, User.GetUserId(), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Permanently deletes the authenticated account after verifying the
    /// current password. Removes the user's borrowing history and notifications.
    /// </summary>
    /// <param name="request">Password confirmation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>204 on success; 400/401/404 on invalid input, wrong password or unknown user.</returns>
    [HttpDelete("account")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.DeleteAccountAsync(User.GetUserId(), request, cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }
}
