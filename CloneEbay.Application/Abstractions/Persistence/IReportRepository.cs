using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Abstractions.Persistence;

public sealed record ProductExportRow(
    int ProductId, string Title, decimal Price, int Quantity, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record OrderExportRow(
    int OrderId, string Status, string BuyerUsername, string BuyerEmail, decimal Total,
    DateTime OrderedAtUtc, DateTime? PaidAtUtc, DateTime? ShippedAtUtc, int ItemCount, string Items);

public interface IReportRepository
{
    Task AddDownloadAsync(ReportDownload download, CancellationToken cancellationToken = default);

    Task<ReportDownload?> GetDownloadByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ReportDownload> Items, int Total)> ListDownloadsAsync(
        int userId, string? source, ReportDownloadStatus? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveScheduleAsync(int userId, string source, string type, CancellationToken cancellationToken = default);

    Task AddScheduleAsync(ReportSchedule schedule, CancellationToken cancellationToken = default);

    Task<ReportSchedule?> GetScheduleByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ReportSchedule> Items, int Total)> ListSchedulesAsync(
        int userId, string? source, string? type, ReportScheduleFrequency? frequency, bool onlyActive,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportSchedule>> GetDueSchedulesAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductExportRow>> GetListingsForExportAsync(
        int sellerId, ProductStatus[] statuses, int maxRows, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderExportRow>> GetOrdersForExportAsync(
        int sellerId, DateTime fromUtc, DateTime toUtc, string[]? statusFilter, int maxRows, CancellationToken cancellationToken = default);
}
