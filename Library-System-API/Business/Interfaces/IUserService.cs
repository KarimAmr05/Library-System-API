using LibrarySystem.Business.DTOs.Users;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.Business.Interfaces;

/// <summary>
/// Admin-only user management workflows (list, create with role, role change,
/// account activation). Authorization is enforced by the controller; the
/// service enforces business rules such as keeping at least one active admin.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Returns a paged, searchable list of users (safe fields only).
    /// </summary>
    /// <param name="query">Paging/search parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged list of users.</returns>
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(UsersQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new account with an explicit role (used for "Create Admin").
    /// </summary>
    /// <param name="request">New account details including the role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created user projection, or a failure result.</returns>
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's role. Refuses to remove the last remaining admin.
    /// </summary>
    /// <param name="userId">Identifier of the target user.</param>
    /// <param name="request">The new role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated user projection, or a failure result.</returns>
    Task<Result<UserDto>> UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a user account. Refuses to deactivate the
    /// caller themselves or the last remaining active admin.
    /// </summary>
    /// <param name="userId">Identifier of the target user.</param>
    /// <param name="request">The new account status.</param>
    /// <param name="callerId">Identifier of the authenticated admin performing the change.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated user projection, or a failure result.</returns>
    Task<Result<UserDto>> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequestDto request, Guid callerId, CancellationToken cancellationToken = default);
}