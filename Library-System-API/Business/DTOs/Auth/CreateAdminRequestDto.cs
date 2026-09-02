using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Auth;

/// <summary>
/// Request payload for the one-time <c>POST /api/auth/create-admin</c>
/// bootstrap endpoint. Only usable while the database contains no admin.
/// </summary>
public class CreateAdminRequestDto
{
    /// <summary>Gets the display name for the administrator.</summary>
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
}