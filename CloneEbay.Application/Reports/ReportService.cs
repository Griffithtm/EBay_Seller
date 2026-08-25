using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Reports;

/// <summary>
/// Implements spec Part A, with the one mandated deviation from the reference project:
/// a <see cref="ReportDownload"/> row is persisted as <c>Pending</c> BEFORE generation
/// runs, then updated to Completed/Failed — so every request leaves an audit trail even
/// when generation fails (the original never persisted anything on failure; spec A.4/A.8 #1
/// flags this as a bug to fix, not a behaviour to copy).
/// </summary>
public sealed class ReportService(
    IReportRepository reports,
    IReportDownloadGenerator generator,
    IUnitOfWork unitOfWork,
    IClock clock) : IReportService
{
    private static readonly HashSet<string> ValidSources = new(StringComparer.OrdinalIgnoreCase) { ReportSource.Listings, ReportSource.Orders };
    private static readonly HashSet<string> ValidListingTypes = new(StringComparer.OrdinalIgnoreCase)
        { ListingReportType.AllActive, ListingReportType.OutOfStock, ListingReportType.Hidden, ListingReportType.AuctionEnded };
    private static readonly HashSet<string> ValidOrderTypes = new(StringComparer.OrdinalIgnoreCase)
        { OrderReportType.AllOrders, OrderReportType.WaitingPayment, OrderReportType.Confirmed, OrderReportType.Shipped, OrderReportType.Delivered, OrderReportType.Cancelled };
    private static readonly HashSet<string> ValidFrequencies = new(StringComparer.OrdinalIgnoreCase) { "Hourly", "Daily", "Weekly", "Monthly" };

    public async Task<ReportDownloadResponse> CreateDownloadAsync(int userId, CreateReportDownloadRequest request, CancellationToken cancellationToken = default)
    {
        var source = ValidateSourceAndType(request.Source, request.Type, out var type);
        if (request.RangeStartUtc.HasValue && request.RangeEndUtc.HasValue && request.RangeEndUtc < request.RangeStartUtc)
        {
            throw new AppProblemException(422, "REPORT_RANGE_INVALID", "RangeEndUtc must not be before RangeStartUtc.");
        }
        if (request.TimeZone is { Length: > 64 })
        {
            throw new AppProblemException(422, "REPORT_TIMEZONE_INVALID", "TimeZone must be 64 characters or fewer.");
        }

        var now = clock.UtcNow;
        var download = new ReportDownload
        {
            UserId = userId,
            ReferenceCode = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            Source = source,
            Type = type,
            Status = ReportDownloadStatus.Pending,
            RequestedAtUtc = now,
            RangeStartUtc = request.RangeStartUtc,
            RangeEndUtc = request.RangeEndUtc,
            RangeTimeZone = request.TimeZone
        };
        await reports.AddDownloadAsync(download, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var generated = await generator.GenerateAsync(userId, source, type, request.RangeStartUtc, request.RangeEndUtc, cancellationToken);
            download.FileUrl = generated.FileUrl;
            download.CompletedAtUtc = clock.UtcNow;
            download.Status = ReportDownloadStatus.Completed;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (ReportDownloadStatusPolicy.CanMarkFailed(download.Status))
            {
                download.Status = ReportDownloadStatus.Failed;
                download.CompletedAtUtc = clock.UtcNow;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            throw;
        }

        return Map(download);
    }

    public async Task<PagedResult<ReportDownloadResponse>> ListDownloadsAsync(int userId, ReportDownloadListQuery query, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = SanitizePaging(query.Page, query.PageSize);
        ReportDownloadStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<ReportDownloadStatus>(query.Status, true, out var parsed))
            {
                throw new AppProblemException(400, "INVALID_REPORT_STATUS", "Report download status is invalid.");
            }
            status = parsed;
        }

        var (items, total) = await reports.ListDownloadsAsync(userId, query.Source, status, query.FromUtc, query.ToUtc, page, pageSize, cancellationToken);
        return new PagedResult<ReportDownloadResponse>(items.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<ReportScheduleResponse> CreateScheduleAsync(int userId, CreateReportScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var source = ValidateSourceAndType(request.Source, request.Type, out var type);
        if (!ValidFrequencies.Contains(request.Frequency))
        {
            throw new AppProblemException(422, "REPORT_FREQUENCY_INVALID", $"'{request.Frequency}' is not a valid frequency.");
        }
        var frequency = Enum.Parse<ReportScheduleFrequency>(request.Frequency, true);

        var now = clock.UtcNow;
        string? deliveryEmail = null;
        if (!string.IsNullOrWhiteSpace(request.DeliveryEmail))
        {
            var trimmed = request.DeliveryEmail.Trim();
            if (trimmed.Length > 256 || !trimmed.Contains('@'))
            {
                throw new AppProblemException(422, "REPORT_DELIVERY_EMAIL_INVALID", "DeliveryEmail must be a valid email of 256 characters or fewer.");
            }
            deliveryEmail = trimmed.ToLowerInvariant();
        }
        if (request.EndDateUtc.HasValue && request.EndDateUtc.Value.Date < now.Date)
        {
            throw new AppProblemException(422, "REPORT_END_DATE_INVALID", "EndDateUtc cannot be in the past.");
        }

        if (await reports.ExistsActiveScheduleAsync(userId, source, type, cancellationToken))
        {
            throw new AppProblemException(409, "REPORT_SCHEDULE_DUPLICATE", "An active schedule already exists for this source/type.");
        }

        var schedule = new ReportSchedule
        {
            UserId = userId,
            Source = source,
            Type = type,
            Frequency = frequency,
            CreatedAtUtc = now,
            NextRunAtUtc = ReportScheduleCalculator.CalculateNextRun(frequency, now),
            EndDateUtc = request.EndDateUtc,
            IsActive = true,
            DeliveryEmail = deliveryEmail
        };
        await reports.AddScheduleAsync(schedule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(schedule);
    }

    public async Task<PagedResult<ReportScheduleResponse>> ListSchedulesAsync(int userId, ReportScheduleListQuery query, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = SanitizePaging(query.Page, query.PageSize);
        ReportScheduleFrequency? frequency = null;
        if (!string.IsNullOrWhiteSpace(query.Frequency))
        {
            if (!Enum.TryParse<ReportScheduleFrequency>(query.Frequency, true, out var parsed))
            {
                throw new AppProblemException(400, "INVALID_REPORT_FREQUENCY", "Report schedule frequency is invalid.");
            }
            frequency = parsed;
        }

        var (items, total) = await reports.ListSchedulesAsync(userId, query.Source, query.Type, frequency, query.OnlyActive, page, pageSize, cancellationToken);
        return new PagedResult<ReportScheduleResponse>(items.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<ReportScheduleResponse> DeactivateScheduleAsync(int userId, int scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await reports.GetScheduleByIdAsync(scheduleId, userId, cancellationToken)
            ?? throw new AppProblemException(404, "REPORT_SCHEDULE_NOT_FOUND", "Report schedule was not found.");
        schedule.IsActive = false;
        schedule.EndDateUtc ??= clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(schedule);
    }

    public async Task<int> RunDueSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var due = await reports.GetDueSchedulesAsync(now, cancellationToken);
        var executed = 0;
        foreach (var schedule in due)
        {
            try
            {
                await generator.GenerateAsync(schedule.UserId, schedule.Source, schedule.Type, null, null, cancellationToken);
            }
            catch
            {
                // A single seller's misconfigured/broken schedule must not block everyone else's run.
                // (No email infrastructure exists in this codebase yet, so delivery itself is a
                // known gap — see the executor's own remarks for what's left to wire up.)
            }
            schedule.LastRunAtUtc = now;
            schedule.NextRunAtUtc = ReportScheduleCalculator.CalculateNextRun(schedule.Frequency, now);
            executed++;
        }
        if (executed > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
        return executed;
    }

    private static string ValidateSourceAndType(string source, string type, out string normalizedType)
    {
        var normalizedSource = source.Trim().ToLowerInvariant();
        if (!ValidSources.Contains(normalizedSource))
        {
            throw new AppProblemException(422, "UNSUPPORTED_REPORT_SOURCE", $"'{source}' is not a supported report source.");
        }

        normalizedType = type.Trim().ToLowerInvariant();
        var validTypes = normalizedSource == ReportSource.Listings ? ValidListingTypes : ValidOrderTypes;
        if (!validTypes.Contains(normalizedType))
        {
            throw new AppProblemException(422, "UNRECOGNIZED_REPORT_TYPE", $"'{type}' is not a recognized report type for source '{source}'.");
        }
        return normalizedSource;
    }

    private static (int Page, int PageSize) SanitizePaging(int page, int pageSize) =>
        (page <= 0 ? 1 : page, pageSize <= 0 ? 25 : Math.Min(pageSize, 200));

    private static ReportDownloadResponse Map(ReportDownload d) => new(
        d.Id, d.ReferenceCode, d.Source, d.Type, d.Status.ToString(), d.RequestedAtUtc, d.CompletedAtUtc, d.FileUrl, d.RangeStartUtc, d.RangeEndUtc);

    private static ReportScheduleResponse Map(ReportSchedule s) => new(
        s.Id, s.Source, s.Type, s.Frequency.ToString(), s.CreatedAtUtc, s.LastRunAtUtc, s.NextRunAtUtc, s.EndDateUtc, s.IsActive, s.DeliveryEmail);
}
