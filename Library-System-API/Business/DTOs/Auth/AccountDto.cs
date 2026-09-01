using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Auth;

/// <summary>
/// Request payload for initiating a password reset. The response is always
/// generic to prevent account enumeration.
/// </summary>
public class ForgotPasswordRequestDto
{
    /// <summary>Gets or sets the email address to send the reset link to.</summary>
    [Required, EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Request payload for completing a password reset with an emailed token.
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>Gets or sets the email address of the account.</summary>
    [Required, EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or sets the raw token received by email.</summary>
    [Required]
    [MaxLength(200)]
    public string Token { get; init; } = string.Empty;

    /// <summary>Gets or sets the replacement password.</summary>
    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Request payload for permanent self-service account deletion. Requires the
/// current password as proof of identity.
/// </summary>
public class DeleteAccountRequestDto
{
    /// <summary>Gets or sets the account password used to confirm deletion.</summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}
