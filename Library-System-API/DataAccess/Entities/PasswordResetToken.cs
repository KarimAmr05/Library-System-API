namespace LibrarySystem.DataAccess.Entities;

/// <summary>
/// One-time token for password resets. Only the SHA-256 hash of the emailed
/// token is stored; tokens expire and are single-use.
/// </summary>
public class PasswordResetToken
{
    /// <summary>Gets or sets the unique identifier of the token row.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user the token belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the SHA-256 hash of the emailed token value.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC instant after which the token is invalid.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Gets or sets the UTC instant the token was consumed; null while unused.</summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC instant the token was issued.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the owning user navigation.</summary>
    public User User { get; set; } = null!;
}
