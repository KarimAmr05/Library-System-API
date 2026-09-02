using LibrarySystem.API.Extensions;
using LibrarySystem.Business.DTOs.Users;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.Controllers;

/// <summary>
/// Admin-only user management: list users, create accounts with explicit
/// roles (e.g. additional admins), change roles and toggle account status.
/// Business rules (e.g. never removing the last admin) live in the service.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Lists users with paging and an optional name/email search.
    /// </summary>
    /// <param name="query">Paging/search parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged list of users (safe fields only).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers([FromQuery] UsersQueryDto query, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Creates a new account with an explicit role — used to add additional
    /// administrators once the first admin exists.
    /// </summary>
    /// <param name="request">New account details including the role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>201 with the created user projection.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(request, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Created(string.Empty, result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Changes a user's role. The last remaining admin cannot be demoted.
    /// </summary>
    /// <param name="userId">Identifier of the target user.</param>
    /// <param name="request">The new role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated user projection.</returns>
    [HttpPut("{userId:guid}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserRoleAsync(userId, request, cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Activates or deactivates a user account. An admin cannot deactivate
    /// themselves or the last remaining active admin.
    /// </summary>
    /// <param name="userId">Identifier of the target user.</param>
    /// <param name="request">The new account status.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated user projection.</returns>
    [HttpPut("{userId:guid}/status")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserStatusAsync(userId, request, User.GetUserId(), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }
}