namespace LibrarySystem.Business.DTOs.Auth;

/// <summary>
/// Response payload returned after successful authentication.
/// </summary>
/// <param name="Token">The signed JWT bearer token.</param>
/// <param name="ExpiresAtUtc">UTC timestamp at which the token expires.</param>
/// <param name="UserId">Identifier of the authenticated user.</param>
/// <param name="Email">Email of the authenticated user.</param>
/// <param name="Role">Role assigned to the authenticated user.</param>
public sealed record LoginResponseDto(
    string Token,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string Role);
