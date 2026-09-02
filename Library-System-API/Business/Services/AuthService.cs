using LibrarySystem.Business.DTOs.Auth;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Authentication;
using LibrarySystem.Shared.Configuration;
using LibrarySystem.Shared.Constants;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Results;
using LibrarySystem.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Business.Services;

/// <summary>
/// Default authentication service validating credentials against stored users
/// and issuing JWTs through <see cref="IJwtService"/>.
/// </summary>
/// <param name="unitOfWork">Unit of work for user lookups.</param>
/// <param name="jwtService">Token issuance service.</param>
/// <param name="jwtSettings">JWT configuration used to mirror token expiry.</param>
/// <param name="emailSender">Best-effort email delivery for reset links.</param>
/// <param name="appSettings">Application settings (frontend base URL for links).</param>
public sealed class AuthService(
    IUnitOfWork unitOfWork,
    IJwtService jwtService,
    IOptions<JwtSettings> jwtSettings,
    IEmailSender emailSender,
    IOptions<AppSettings> appSettings) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IJwtService _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
    private readonly JwtSettings _jwtSettings =
        jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    private readonly IEmailSender _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
    private readonly AppSettings _appSettings =
        appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));

    /// <summary>Password reset tokens stay valid for this long.</summary>
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    public async Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<LoginResponseDto>([.. validation.Errors]);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        // Identical failure for unknown email and wrong password to avoid
        // account enumeration.
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResponseDto>(
                new Error(ErrorCodes.Unauthorized, "Invalid email or password."));
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponseDto>(
                new Error(ErrorCodes.Forbidden, "This account has been deactivated."));
        }

        return Result.Success(await IssueTokenPairAsync(user, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<LoginResponseDto>([.. validation.Errors]);
        }

        // Emails are stored normalized (trimmed + lowercase) exactly like the
        // seeder does, so uniqueness checks stay case-insensitive.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _unitOfWork.Users.ExistsAsync(u => u.Email == normalizedEmail, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<LoginResponseDto>(
                Error.Conflict("An account with this email already exists."));
        }

        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = utcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(await IssueTokenPairAsync(user, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure([.. validation.Errors]);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        // Identical generic response whether or not the account exists —
        // never reveal which emails are registered.
        if (user is null)
        {
            return Result.Success();
        }

        // Only one live token per user: revoke previous unused ones.
        var previous = await _unitOfWork.PasswordResetTokens.Query()
            .Where(t => t.UserId == user.Id && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var utcNow = DateTime.UtcNow;
        foreach (var stale in previous)
        {
            _unitOfWork.PasswordResetTokens.Remove(stale);
        }

        var rawToken = SecureTokenGenerator.Generate();
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = SecureTokenGenerator.Hash(rawToken),
            ExpiresAtUtc = utcNow.Add(ResetTokenLifetime),
            CreatedAtUtc = utcNow
        };

        await _unitOfWork.PasswordResetTokens.AddAsync(resetToken, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var resetLink =
            $"{_appSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(normalizedEmail)}";

        await _emailSender.SendAsync(
            user.Email,
            "Library System — reset your password",
            $"Hello {user.FullName},\n\n" +
            "We received a request to reset your Library System password.\n" +
            "Open the link below to choose a new password (valid for 30 minutes):\n\n" +
            resetLink + "\n\n" +
            "If you did not request this, you can safely ignore this email.",
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure([.. validation.Errors]);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var tokenHash = SecureTokenGenerator.Hash(request.Token);

        var resetToken = await _unitOfWork.PasswordResetTokens.Query()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (resetToken is null || resetToken.UsedAtUtc != null || resetToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Result.Failure(
                Error.BusinessRule("This reset link is invalid or has expired. Please request a new one."));
        }

        var user = await _unitOfWork.Users.GetByIdTrackedAsync(resetToken.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || user.Email != normalizedEmail)
        {
            return Result.Failure(
                Error.BusinessRule("This reset link is invalid or has expired. Please request a new one."));
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        resetToken.UsedAtUtc = DateTime.UtcNow;
        // The token came from an untracked read — mark it modified explicitly.
        _unitOfWork.PasswordResetTokens.Update(resetToken);

        // Password changed: revoke every active refresh token so stolen
        // sessions cannot survive a reset.
        await RevokeActiveRefreshTokensAsync(user.Id, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAccountAsync(
        Guid userId,
        DeleteAccountRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure([.. validation.Errors]);
        }

        var user = await _unitOfWork.Users.GetByIdTrackedAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(Error.NotFound("User"));
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Wrong-password proof: same shape as a failed sign-in.
            return Result.Failure(
                new Error(ErrorCodes.Unauthorized, "Incorrect password. The account was not deleted."));
        }

        // Remove owned history so the User FK (Restrict) never blocks deletion.
        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var requests = await _unitOfWork.BorrowingRequests.Query()
                .Where(r => r.UserId == userId)
                .ToListAsync(token)
                .ConfigureAwait(false);

            foreach (var borrowingRequest in requests)
            {
                _unitOfWork.BorrowingRequests.Remove(borrowingRequest);
            }

            var notifications = await _unitOfWork.Notifications.Query()
                .Where(n => n.RecipientUserId == userId)
                .ToListAsync(token)
                .ConfigureAwait(false);

            foreach (var notification in notifications)
            {
                _unitOfWork.Notifications.Remove(notification);
            }

            var tokens = await _unitOfWork.PasswordResetTokens.Query()
                .Where(t => t.UserId == userId)
                .ToListAsync(token)
                .ConfigureAwait(false);

            foreach (var passwordResetToken in tokens)
            {
                _unitOfWork.PasswordResetTokens.Remove(passwordResetToken);
            }

            _unitOfWork.Users.Remove(user);
            await _unitOfWork.SaveChangesAsync(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponseDto>> CreateAdminAsync(
        CreateAdminRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<LoginResponseDto>([.. validation.Errors]);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Serializable transaction: concurrent bootstrap requests serialize on
        // the Users table, so exactly one request can observe "no admin exists"
        // and insert; every other request (or a retry) hits the conflict below.
        return await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await _unitOfWork.Users.ExistsAsync(u => u.Role == UserRole.Admin, token).ConfigureAwait(false))
            {
                return Result.Failure<LoginResponseDto>(Error.Conflict(
                    "An administrator account already exists. Admin bootstrap is no longer available."));
            }

            if (await _unitOfWork.Users.ExistsAsync(u => u.Email == normalizedEmail, token).ConfigureAwait(false))
            {
                return Result.Failure<LoginResponseDto>(
                    Error.Conflict("An account with this email already exists."));
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.Name.Trim(),
                Email = normalizedEmail,
                // Same PBKDF2 hashing used by registration/login.
                PasswordHash = PasswordHasher.Hash(request.Password),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user, token).ConfigureAwait(false);
            return Result.Success(await IssueTokenPairAsync(user, token).ConfigureAwait(false));
        }, System.Data.IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<LoginResponseDto>([.. validation.Errors]);
        }

        var tokenHash = SecureTokenGenerator.Hash(request.RefreshToken);

        // Validation + rotation run inside one transaction so the same token
        // presented twice concurrently can only be consumed a single time.
        return await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var storedToken = await _unitOfWork.RefreshTokens.Query()
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, token)
                .ConfigureAwait(false);

            // One generic message for missing/expired/revoked/unknown-user
            // cases — never reveal why a refresh token was rejected.
            if (storedToken is null ||
                storedToken.RevokedAtUtc is not null ||
                storedToken.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return Result.Failure<LoginResponseDto>(
                    new Error(ErrorCodes.Unauthorized, "Invalid refresh token."));
            }

            var user = await _unitOfWork.Users.Query()
                .FirstOrDefaultAsync(u => u.Id == storedToken.UserId, token)
                .ConfigureAwait(false);

            if (user is null || !user.IsActive)
            {
                return Result.Failure<LoginResponseDto>(
                    new Error(ErrorCodes.Unauthorized, "Invalid refresh token."));
            }

            // Rotation: revoke the presented token, then issue a fresh pair.
            // The token is fetched tracked so the change is persisted even if
            // the same context instance already tracks this row.
            var trackedToken = await _unitOfWork.RefreshTokens
                .GetByIdTrackedAsync(storedToken.Id, token)
                .ConfigureAwait(false);

            trackedToken!.RevokedAtUtc = DateTime.UtcNow;

            return Result.Success(await IssueTokenPairAsync(user, token).ConfigureAwait(false));
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> RevokeRefreshTokenAsync(
        string refreshToken,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = SecureTokenGenerator.Hash(refreshToken);

        var storedToken = await _unitOfWork.RefreshTokens.Query()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is not null &&
            storedToken.UserId == userId &&
            storedToken.RevokedAtUtc is null)
        {
            var trackedToken = await _unitOfWork.RefreshTokens
                .GetByIdTrackedAsync(storedToken.Id, cancellationToken)
                .ConfigureAwait(false);

            trackedToken!.RevokedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Idempotent logout: unknown/foreign/already-revoked tokens still
        // succeed so no information about token existence is revealed.
        return Result.Success();
    }

    /// <summary>
    /// Revokes every active refresh token belonging to a user.
    /// </summary>
    /// <param name="userId">Owner of the tokens.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task RevokeActiveRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeIds = await _unitOfWork.RefreshTokens.Query()
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var revokedAt = DateTime.UtcNow;
        foreach (var activeId in activeIds)
        {
            var activeToken = await _unitOfWork.RefreshTokens
                .GetByIdTrackedAsync(activeId, cancellationToken)
                .ConfigureAwait(false);

            if (activeToken is not null)
            {
                activeToken.RevokedAtUtc = revokedAt;
            }
        }
    }

    /// <summary>
    /// Issues a fresh access-token/refresh-token pair for the user and
    /// persists the refresh-token hash for later validation.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The complete authentication response payload.</returns>
    private async Task<LoginResponseDto> IssueTokenPairAsync(User user, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var accessTokenExpiresAtUtc = utcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
        var refreshTokenExpiresAtUtc = utcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var rawRefreshToken = SecureTokenGenerator.Generate();

        await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            // Only the SHA-256 hash is stored, mirroring PasswordResetToken.
            TokenHash = SecureTokenGenerator.Hash(rawRefreshToken),
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
            CreatedAtUtc = utcNow
        }, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var accessToken = _jwtService.GenerateToken(user.Id, user.Email, user.Role);

        return new LoginResponseDto(
            accessToken,
            accessTokenExpiresAtUtc,
            rawRefreshToken,
            refreshTokenExpiresAtUtc,
            user.Id,
            user.Email,
            user.Role.ToString());
    }
}
