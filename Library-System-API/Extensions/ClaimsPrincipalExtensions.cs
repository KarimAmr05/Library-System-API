using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LibrarySystem.Shared.Enums;

namespace LibrarySystem.API.Extensions;

/// <summary>
/// Extracts identity information from the authenticated principal so that
/// client-supplied user/admin identifiers are never trusted over JWT claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Resolves the authenticated user's identifier from claims.
    /// </summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The parsed user id.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when no valid id claim exists.</exception>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("The token does not contain a valid user identifier.");
        }

        return userId;
    }

    /// <summary>
    /// Resolves the authenticated role from claims.
    /// </summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The parsed role.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when no valid role claim exists.</exception>
    public static UserRole GetRole(this ClaimsPrincipal principal)
    {
        if (!Enum.TryParse<UserRole>(principal.FindFirst(ClaimTypes.Role)?.Value, ignoreCase: true, out var role))
        {
            throw new UnauthorizedAccessException("The token does not contain a valid role claim.");
        }

        return role;
    }
}
