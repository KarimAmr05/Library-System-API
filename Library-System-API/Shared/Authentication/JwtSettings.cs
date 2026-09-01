using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Shared.Authentication;

/// <summary>
/// Strongly-typed JWT configuration bound from the "Jwt" configuration section.
/// Secrets must be supplied via environment variables or user secrets — never committed.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Gets the configuration section name this class binds to.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the symmetric signing key. Minimum length 32 characters.
    /// Supplied via configuration/user secrets in real deployments.
    /// </summary>
    [Required]
    [MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer claim embedded in generated tokens.
    /// </summary>
    [Required]
    public string Issuer { get; init; } = "LibrarySystem";

    /// <summary>
    /// Gets or sets the audience claim embedded in generated tokens.
    /// </summary>
    [Required]
    public string Audience { get; init; } = "LibrarySystemClient";

    /// <summary>
    /// Gets or sets the token lifetime in minutes. Defaults to 120 minutes.
    /// </summary>
    [Range(1, 1440)]
    public int ExpiryMinutes { get; init; } = 120;
}
