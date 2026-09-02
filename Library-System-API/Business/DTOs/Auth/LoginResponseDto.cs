namespace LibrarySystem.Business.DTOs.Auth;

/// <summary>
/// Response payload returned after successful authentication (login, register,
/// admin bootstrap and refresh-token exchange). Carries the access-token/JWT
/// pair used by the frontend session flow.
/// </summary>
/// <param name="Token">The signed JWT bearer (access) token.</param>
/// <param name="ExpiresAtUtc">UTC timestamp at which the access token expires.</param>
/// <param name="RefreshToken">The opaque refresh token used to renew the pair.</param>
/// <param name="RefreshTokenExpiresAtUtc">UTC timestamp at which the refresh token expires.</param>
/// <param name="UserId">Identifier of the authenticated user.</param>
/// <param name="Email">Email of the authenticated user.</param>
/// <param name="Role">Role assigned to the authenticated user.</param>
public sealed record LoginResponseDto(
    string Token,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    Guid UserId,
    string Email,
    string Role);