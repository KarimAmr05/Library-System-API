namespace LibrarySystem.DataAccess.Entities;

/// <summary>
/// Refresh token enabling silent session renewal. Only the SHA-256 hash of the
/// raw token value is stored; tokens expire and are revoked on use (rotation)
/// or logout.
/// </summary>
public class RefreshToken
{
    /// <summary>Gets or sets the unique identifier of the token row.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user the token belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the SHA-256 hash of the issued token value.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC instant after which the token is invalid.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Gets or sets the UTC instant the token was issued.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC instant the token was revoked (used or logged out); null while active.</summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Gets or sets the owning user navigation.</summary>
    public User User { get; set; } = null!;
}