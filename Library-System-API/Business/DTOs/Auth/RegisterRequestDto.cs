using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Auth;

/// <summary>
/// Request payload for self-service account registration.
/// Registered accounts are always created in the <c>User</c> role; admin
/// accounts are provisioned through seeding/administration only.
/// </summary>
public class RegisterRequestDto
{
    /// <summary>Gets or sets the display name shown in the application.</summary>
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets or sets the email address used for sign-in. Must be unique.</summary>
    [Required, EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or sets the plaintext password (transport-secured via TLS only).</summary>
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
