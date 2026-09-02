using LibrarySystem.Business.DTOs.Auth;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.Business.Interfaces;

/// <summary>
/// Authentication workflow abstraction.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user by email/password and issues a signed JWT.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The login response carrying the token, or a failure result.</returns>
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user account (role <c>User</c>), persists it and issues
    /// a signed JWT so the caller is signed in immediately.
    /// </summary>
    /// <param name="request">Registration details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The login response carrying the token, or a failure result.</returns>
    Task<Result<LoginResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a password reset: issues a single-use token and emails a
    /// reset link. Always succeeds from the caller's perspective to prevent
    /// account enumeration.
    /// </summary>
    /// <param name="request">The email requesting a reset.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result regardless of account existence.</returns>
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a password reset: consumes the emailed token and replaces
    /// the account password.
    /// </summary>
    /// <param name="request">Email, raw token and the new password.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result, or a failure result for invalid/expired tokens.</returns>
    Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes the account (and its borrowing history and
    /// notifications) after verifying the current password.
    /// </summary>
    /// <param name="userId">Identifier of the authenticated user.</param>
    /// <param name="request">Password confirmation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result, or a failure result for a wrong password.</returns>
    Task<Result> DeleteAccountAsync(Guid userId, DeleteAccountRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// One-time bootstrap: creates the very first administrator account.
    /// Refuses (409) once any admin account exists.
    /// </summary>
    /// <param name="request">Admin account details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result with the issued token pair, or a conflict failure.</returns>
    Task<Result<LoginResponseDto>> CreateAdminAsync(CreateAdminRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a valid refresh token for a fresh access/refresh token pair,
    /// revoking (rotating) the presented token so it cannot be replayed.
    /// </summary>
    /// <param name="request">The refresh token to exchange.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result with the new token pair, or an unauthorized failure.</returns>
    Task<Result<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the caller's refresh token so it can no longer be refreshed
    /// (logout). Idempotent: unknown or already-revoked tokens still succeed.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token to revoke.</param>
    /// <param name="userId">Identifier of the authenticated caller.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result.</returns>
    Task<Result> RevokeRefreshTokenAsync(string refreshToken, Guid userId, CancellationToken cancellationToken = default);
}
