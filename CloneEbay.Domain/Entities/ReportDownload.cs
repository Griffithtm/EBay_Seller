using CloneEbay.Domain.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Domain.Entities;

/// <summary>
/// One immediate, one-off report export (CSV). Distinct from <see cref="ReportSchedule"/>
/// (a recurring configuration) — different lifecycle, different table (spec Part A.1).
/// </summary>
public sealed class ReportDownload : BaseEntity
{
    public int UserId { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public ReportDownloadStatus Status { get; set; } = ReportDownloadStatus.Pending;
    public string? FileUrl { get; set; }
    public DateTime? RangeStartUtc { get; set; }
    public DateTime? RangeEndUtc { get; set; }
    public string? RangeTimeZone { get; set; }

    public User User { get; set; } = null!;
}

/// <summary>
/// Pure state-machine rules for <see cref="ReportDownload.Status"/> (spec A.2) — a finished
/// download (Completed/Failed) never transitions again. Kept dependency-free and separate
/// from the entity so it is trivially unit-testable, mirroring the <see cref="ReturnRequestStatus"/>
/// and <see cref="SellingLimitPolicy"/> pattern already used in this codebase.
/// </summary>
public static class ReportDownloadStatusPolicy
{
    public static bool CanMarkProcessing(ReportDownloadStatus current) =>
        current is ReportDownloadStatus.Pending or ReportDownloadStatus.Processing;

    public static bool CanMarkCompleted(ReportDownloadStatus current) =>
        current != ReportDownloadStatus.Failed;

    public static bool CanMarkFailed(ReportDownloadStatus current) =>
        current != ReportDownloadStatus.Completed;
}
