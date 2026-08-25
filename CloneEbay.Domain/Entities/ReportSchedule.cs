using CloneEbay.Domain.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Domain.Entities;

/// <summary>
/// A recurring report-export configuration (spec A.1/A.3). Only a config record — the
/// actual file is produced later by a background executor (spec A.8 #3), not at creation time.
/// </summary>
public sealed class ReportSchedule : BaseEntity
{
    public int UserId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ReportScheduleFrequency Frequency { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastRunAtUtc { get; set; }
    public DateTime? NextRunAtUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DeliveryEmail { get; set; }

    public User User { get; set; } = null!;
}

/// <summary>Pure scheduling arithmetic (spec A.3) — no dependencies, fully unit-testable.</summary>
public static class ReportScheduleCalculator
{
    public static DateTime CalculateNextRun(ReportScheduleFrequency frequency, DateTime fromUtc) => frequency switch
    {
        ReportScheduleFrequency.Hourly => fromUtc.AddHours(1),
        ReportScheduleFrequency.Daily => fromUtc.AddDays(1),
        ReportScheduleFrequency.Weekly => fromUtc.AddDays(7),
        ReportScheduleFrequency.Monthly => fromUtc.AddMonths(1),
        _ => fromUtc.AddDays(1)
    };
}
