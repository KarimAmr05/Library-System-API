using System.ComponentModel.DataAnnotations;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.Shared.Enums;

namespace LibrarySystem.Business.DTOs.Users;

/// <summary>
/// Safe user projection for admin user management. Never exposes password
/// hashes or tokens.
/// </summary>
/// <param name="Id">Identifier of the user.</param>
/// <param name="Name">Display name of the user.</param>
/// <param name="Email">Email address used for sign-in.</param>
/// <param name="Role">Assigned role.</param>
/// <param name="IsActive">Whether the account may access the system.</param>
/// <param name="CreatedAt">UTC timestamp the account was created.</param>
public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt)
{
    /// <summary>Projects a <see cref="User"/> entity.</summary>
    public static UserDto FromEntity(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.Role.ToString(),
        user.IsActive,
        user.CreatedAt);
}

/// <summary>
/// Query parameters for <c>GET /api/admin/users</c>.
/// </summary>
public class UsersQueryDto
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>Gets or sets the 1-based page index (minimum 1).</summary>
    [Range(1, int.MaxValue)]
    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }

    /// <summary>Gets or sets the page size. Defaults to 20 and is capped at 100.</summary>
    [Range(1, MaxPageSize)]
    public int PageSize { get => _pageSize; set => _pageSize = value is < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize); }

    /// <summary>Gets or sets the optional case-insensitive search over name/email.</summary>
    public string? Search { get; init; }
}

/// <summary>
/// Request payload for <c>POST /api/admin/users</c>: an admin creates a new
/// account with an explicit role. Role must be Admin or User.
/// </summary>
public class CreateUserRequestDto
{
    /// <summary>Gets the display name for the new account.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the email address used for sign-in.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the plaintext password (hashed before persistence).</summary>
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    /// <summary>Gets the role to assign. Admin-only endpoint enforces authorization.</summary>
    [Required]
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; init; } = UserRole.User;
}

/// <summary>
/// Request payload for <c>PUT /api/admin/users/{id}/role</c>.
/// </summary>
public class UpdateUserRoleRequestDto
{
    /// <summary>Gets the new role for the target user.</summary>
    [Required]
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; init; }
}

/// <summary>
/// Request payload for <c>PUT /api/admin/users/{id}/status</c>.
/// </summary>
public class UpdateUserStatusRequestDto
{
    /// <summary>Gets a value indicating whether the account may access the system.</summary>
    [Required]
    public bool IsActive { get; init; }
}