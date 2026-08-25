using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Tests;

public sealed class ReportTests
{
    [Theory]
    [InlineData(ReportDownloadStatus.Pending, true)]
    [InlineData(ReportDownloadStatus.Processing, true)]
    [InlineData(ReportDownloadStatus.Completed, false)]
    [InlineData(ReportDownloadStatus.Failed, false)]
    public void CanMarkProcessing_only_from_non_terminal_states(ReportDownloadStatus current, bool expected)
    {
        Assert.Equal(expected, ReportDownloadStatusPolicy.CanMarkProcessing(current));
    }

    [Theory]
    [InlineData(ReportDownloadStatus.Pending, true)]
    [InlineData(ReportDownloadStatus.Processing, true)]
    [InlineData(ReportDownloadStatus.Completed, true)]
    [InlineData(ReportDownloadStatus.Failed, false)]
    public void CanMarkCompleted_rejects_only_Failed(ReportDownloadStatus current, bool expected)
    {
        Assert.Equal(expected, ReportDownloadStatusPolicy.CanMarkCompleted(current));
    }

    [Theory]
    [InlineData(ReportDownloadStatus.Pending, true)]
    [InlineData(ReportDownloadStatus.Processing, true)]
    [InlineData(ReportDownloadStatus.Completed, false)]
    [InlineData(ReportDownloadStatus.Failed, true)]
    public void CanMarkFailed_rejects_only_Completed(ReportDownloadStatus current, bool expected)
    {
        Assert.Equal(expected, ReportDownloadStatusPolicy.CanMarkFailed(current));
    }

    [Theory]
    [InlineData(ReportScheduleFrequency.Hourly, 1)]
    [InlineData(ReportScheduleFrequency.Daily, 24)]
    [InlineData(ReportScheduleFrequency.Weekly, 24 * 7)]
    public void CalculateNextRun_hour_day_week_add_correct_span(ReportScheduleFrequency frequency, int expectedHours)
    {
        var from = new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc);
        var next = ReportScheduleCalculator.CalculateNextRun(frequency, from);
        Assert.Equal(from.AddHours(expectedHours), next);
    }

    [Fact]
    public void CalculateNextRun_monthly_adds_one_calendar_month()
    {
        var from = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var next = ReportScheduleCalculator.CalculateNextRun(ReportScheduleFrequency.Monthly, from);
        Assert.Equal(from.AddMonths(1), next);
    }
}
