using LibrarySystem.Shared.Enums;

namespace LibrarySystem.Shared.Authentication;

/// <summary>
/// Abstraction over JSON Web Token issuance and identity extraction.
/// Implemented centrally so authentication concerns never leak into
/// controllers or business services.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a signed, expiring JWT for the supplied user identity.
    /// </summary>
    /// <param name="userId">Unique identifier of the authenticated user.</param>
    /// <param name="email">Email address of the authenticated user.</param>
    /// <param name="role">Role used for role-based authorization.</param>
    /// <returns>The serialized JWT string.</returns>
    string GenerateToken(Guid userId, string email, UserRole role);

    /// <summary>
    /// Reads the user identifier embedded in a valid principal's claims.
    /// </summary>
    /// <param name="principal">The authenticated claims principal.</param>
    /// <returns>The parsed user identifier, or <c>null</c> when absent or malformed.</returns>
    Guid? GetUserId(System.Security.Claims.ClaimsPrincipal principal);

    /// <summary>
    /// Reads the role embedded in a valid principal's claims.
    /// </summary>
    /// <param name="principal">The authenticated claims principal.</param>
    /// <returns>The parsed role, or <c>null</c> when absent or malformed.</returns>
    UserRole? GetRole(System.Security.Claims.ClaimsPrincipal principal);
}
