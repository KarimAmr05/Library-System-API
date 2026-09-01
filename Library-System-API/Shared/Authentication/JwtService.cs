using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibrarySystem.Shared.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LibrarySystem.Shared.Authentication;

/// <summary>
/// Default <see cref="IJwtService"/> implementation using HMAC-SHA256 signed tokens
/// configured through <see cref="JwtSettings"/>.
/// </summary>
/// <param name="settings">JWT settings bound from configuration.</param>
public sealed class JwtService(IOptions<JwtSettings> settings) : IJwtService
{
    private readonly JwtSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

    /// <inheritdoc />
    public string GenerateToken(Guid userId, string email, UserRole role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public Guid? GetUserId(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <inheritdoc />
    public UserRole? GetRole(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return Enum.TryParse<UserRole>(principal.FindFirst(ClaimTypes.Role)?.Value,
            ignoreCase: true, out var role)
            ? role
            : null;
    }
}
