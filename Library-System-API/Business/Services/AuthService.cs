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

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);

        return Result.Success(new LoginResponseDto(token, expiresAtUtc, user.Id, user.Email,
            user.Role.ToString()));
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

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);

        return Result.Success(new LoginResponseDto(token, expiresAtUtc, user.Id, user.Email,
            user.Role.ToString()));
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
}
