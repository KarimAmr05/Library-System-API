using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Auth;

/// <summary>
/// Request payload for user authentication.
/// </summary>
public class LoginRequestDto
{
    /// <summary>Gets or sets the user's email address.</summary>
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or sets the user's password in plaintext (transport-secured via TLS only).</summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}
