using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.BackgroundJobs;

/// <summary>
/// Strongly-typed configuration for background jobs, bound from "BackgroundJobs".
/// Intervals are configurable so production timing is never hard-coded.
/// </summary>
public sealed class BackgroundJobSettings
{
    /// <summary>
    /// Gets the configuration section name this class binds to.
    /// </summary>
    public const string SectionName = "BackgroundJobs";

    /// <summary>Gets or sets how often the expiration/reminder job runs.</summary>
    [Range(1, 1440)]
    public int ExpirationCheckIntervalMinutes { get; init; } = 60;

    /// <summary>
    /// Gets or sets how many days before due date a reminder should be sent.
    /// </summary>
    [Range(1, 30)]
    public int ReminderDaysBeforeDue { get; init; } = 3;
}
