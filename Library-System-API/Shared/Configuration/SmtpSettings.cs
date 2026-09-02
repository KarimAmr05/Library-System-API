using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Shared.Configuration;

/// <summary>
/// Strongly-typed SMTP configuration bound from the "Smtp" section.
/// When <see cref="Host"/> is empty the sender falls back to logging the
/// message content, which keeps local development fully testable.
/// </summary>
public sealed class SmtpSettings
{
    /// <summary>Gets the configuration section name this class binds to.</summary>
    public const string SectionName = "Smtp";

    /// <summary>Gets or sets the SMTP server host. Empty disables real sending.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>Gets or sets the SMTP server port.</summary>
    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    /// <summary>Gets or sets the SMTP user name.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Gets or sets the SMTP password. Supply via user secrets in real deployments.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Gets or sets the From address for outgoing mail.</summary>
    [Required]
    public string From { get; init; } = "no-reply@library.local";

    /// <summary>Gets or sets a value indicating whether to use STARTTLS.</summary>
    public bool EnableSsl { get; init; } = true;
}

/// <summary>
/// Application-level settings bound from the "App" section.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Gets the configuration section name this class binds to.</summary>
    public const string SectionName = "App";

    /// <summary>Gets or sets the frontend base URL used in links (password resets).</summary>
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";

    /// <summary>
    /// Gets the browser origins allowed by CORS. SignalR (websockets) and the
    /// Angular dev server are covered here; keep production origins in
    /// configuration rather than hard-coding them.
    /// </summary>
    public string[] CorsOrigins { get; init; } = ["http://localhost:4200"];
}
