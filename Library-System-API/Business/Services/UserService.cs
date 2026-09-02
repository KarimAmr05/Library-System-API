using LibrarySystem.Business.DTOs.Users;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Results;
using LibrarySystem.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Business.Services;

/// <summary>
/// Default admin user-management service operating on the shared
/// <see cref="IUnitOfWork.Users"/> repository.
/// </summary>
/// <param name="unitOfWork">Unit of work for user persistence.</param>
public sealed class UserService(IUnitOfWork unitOfWork) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    /// <inheritdoc />
    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(
        UsersQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var search = query.Search?.Trim().ToLowerInvariant();

        var dataQuery = _unitOfWork.Users.Query();

        if (!string.IsNullOrEmpty(search))
        {
            dataQuery = dataQuery.Where(u =>
                u.FullName.ToLower().Contains(search) || u.Email.Contains(search));
        }

        var totalItems = await dataQuery.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await dataQuery
            .OrderBy(u => u.FullName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(PagedResult<UserDto>.Create(
            [.. items.Select(UserDto.FromEntity)], query.Page, query.PageSize, totalItems));
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> CreateUserAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<UserDto>([.. validation.Errors]);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _unitOfWork.Users.ExistsAsync(u => u.Email == normalizedEmail, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<UserDto>(
                Error.Conflict("An account with this email already exists."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(UserDto.FromEntity(user));
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> UpdateUserRoleAsync(
        Guid userId,
        UpdateUserRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<UserDto>([.. validation.Errors]);
        }

        var user = await _unitOfWork.Users.GetByIdTrackedAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<UserDto>(Error.NotFound("User"));
        }

        if (user.Role == request.Role)
        {
            return Result.Success(UserDto.FromEntity(user));
        }

        if (user.Role == UserRole.Admin && request.Role == UserRole.User)
        {
            var otherActiveAdmins = await _unitOfWork.Users.CountAsync(
                u => u.Role == UserRole.Admin && u.IsActive && u.Id != userId,
                cancellationToken).ConfigureAwait(false);

            if (otherActiveAdmins == 0)
            {
                return Result.Failure<UserDto>(Error.BusinessRule(
                    "The system must keep at least one administrator account."));
            }
        }

        user.Role = request.Role;
        // Role changed: invalidate existing sessions so the next access-token
        // expiry picks up the new role claim.
        await RevokeActiveRefreshTokensAsync(userId, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(UserDto.FromEntity(user));
    }
    /// <inheritdoc />
    public async Task<Result<UserDto>> UpdateUserStatusAsync(
        Guid userId,
        UpdateUserStatusRequestDto request,
        Guid callerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _unitOfWork.Users.GetByIdTrackedAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<UserDto>(Error.NotFound("User"));
        }

        if (user.IsActive == request.IsActive)
        {
            return Result.Success(UserDto.FromEntity(user));
        }

        if (!request.IsActive && userId == callerId)
        {
            return Result.Failure<UserDto>(Error.BusinessRule(
                "You cannot deactivate your own account."));
        }

        if (!request.IsActive && user.Role == UserRole.Admin && user.IsActive)
        {
            var otherActiveAdmins = await _unitOfWork.Users.CountAsync(
                u => u.Role == UserRole.Admin && u.IsActive && u.Id != userId,
                cancellationToken).ConfigureAwait(false);

            if (otherActiveAdmins == 0)
            {
                return Result.Failure<UserDto>(Error.BusinessRule(
                    "The system must keep at least one active administrator account."));
            }
        }

        user.IsActive = request.IsActive;
        // Deactivated accounts lose their sessions immediately.
        await RevokeActiveRefreshTokensAsync(userId, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(UserDto.FromEntity(user));
    }

    /// <summary>
    /// Revokes every active refresh token belonging to a user.
    /// </summary>
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
}