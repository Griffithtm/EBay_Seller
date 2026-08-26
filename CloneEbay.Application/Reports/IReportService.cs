using CloneEbay.Application.Common;

namespace CloneEbay.Application.Reports;

public interface IReportService
{
    Task<ReportDownloadResponse> CreateDownloadAsync(int userId, CreateReportDownloadRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<ReportDownloadResponse>> ListDownloadsAsync(int userId, ReportDownloadListQuery query, CancellationToken cancellationToken = default);

    Task<ReportScheduleResponse> CreateScheduleAsync(int userId, CreateReportScheduleRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<ReportScheduleResponse>> ListSchedulesAsync(int userId, ReportScheduleListQuery query, CancellationToken cancellationToken = default);

    Task<ReportScheduleResponse> DeactivateScheduleAsync(int userId, int scheduleId, CancellationToken cancellationToken = default);

    /// <summary>Background-executor entry point (spec A.8 #3 — no equivalent exists in the reference project; built fresh here).</summary>
    Task<int> RunDueSchedulesAsync(CancellationToken cancellationToken = default);
}
