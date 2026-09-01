using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.Messaging;

/// <summary>
/// Strongly-typed RabbitMQ configuration bound from the "RabbitMq" section.
/// Credentials must be provided via configuration/user secrets in real deployments.
/// </summary>
public sealed class RabbitMqSettings
{
    /// <summary>
    /// Gets the configuration section name this class binds to.
    /// </summary>
    public const string SectionName = "RabbitMq";

    /// <summary>Gets or sets the broker host name.</summary>
    [Required]
    public string HostName { get; init; } = "localhost";

    /// <summary>Gets or sets the broker port.</summary>
    [Range(1, 65535)]
    public int Port { get; init; } = 5672;

    /// <summary>Gets or sets the broker user name.</summary>
    [Required]
    public string UserName { get; init; } = "guest";

    /// <summary>Gets or sets the broker password.</summary>
    [Required]
    public string Password { get; init; } = "guest";

    /// <summary>Gets or sets the durable queue used for borrowing requests.</summary>
    [Required]
    public string BorrowRequestQueue { get; init; } = "borrow-requests";
}
