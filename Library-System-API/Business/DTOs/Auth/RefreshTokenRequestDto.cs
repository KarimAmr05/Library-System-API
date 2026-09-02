using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Auth;

/// <summary>
/// Request payload for <c>POST /api/auth/refresh-token</c>: exchanges a valid
/// refresh token for a fresh access/refresh token pair (rotation).
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>Gets the raw refresh token issued by a previous login/refresh.</summary>
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}