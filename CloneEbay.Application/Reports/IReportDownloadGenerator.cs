namespace CloneEbay.Application.Reports;

public sealed record GeneratedReportFile(string FileUrl, int RowCount);

/// <summary>
/// Produces the CSV content for a report download AND stores it, returning a servable URL.
/// Implemented in Infrastructure — no ASP.NET dependency required, so it can run entirely
/// through Application-owned abstractions (same DI pattern as IProductCreateRateLimiter).
/// </summary>
public interface IReportDownloadGenerator
{
    Task<GeneratedReportFile> GenerateAsync(
        int sellerId,
        string source,
        string type,
        DateTime? rangeStartUtc,
        DateTime? rangeEndUtc,
        CancellationToken cancellationToken = default);
}
