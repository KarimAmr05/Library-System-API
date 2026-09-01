using System.Security.Cryptography;

namespace LibrarySystem.Shared.Security;

/// <summary>
/// Cryptographic helpers for one-time security tokens (password resets).
/// Raw tokens are shown/emailed once; only their SHA-256 hashes are stored.
/// </summary>
public static class SecureTokenGenerator
{
    /// <summary>
    /// Generates a cryptographically random, URL-safe token.
    /// </summary>
    /// <param name="byteCount">Entropy in bytes; 32 yields a 43-char token.</param>
    /// <returns>Base64url-encoded token without padding.</returns>
    public static string Generate(int byteCount = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Computes the SHA-256 hash used for storage-side token comparison.
    /// </summary>
    /// <param name="token">Raw token value.</param>
    /// <returns>Lowercase hex-encoded hash.</returns>
    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
