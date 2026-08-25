using System.Globalization;
using System.Text;
using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Application.Reports;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Infrastructure.Reports;

/// <summary>
/// Adapted from the reference project's ReportDownloadGenerator (spec A.5), with the
/// mandated improvement applied: an unrecognized Type is always a clear error for BOTH
/// Source branches (the original silently returned unfiltered orders for an unknown
/// order-report type, flagged in the spec as a data-leak risk not to be copied).
/// Source/Type are matched against fixed codes (see <see cref="ReportSource"/>), not
/// free-text labels — the other flaw the spec calls out.
/// </summary>
public sealed class ReportDownloadGenerator(IReportRepository reports, IClock clock) : IReportDownloadGenerator
{
    private const int MaxRows = 5000;
    private const int DefaultOrderRangeDays = 90;

    public async Task<GeneratedReportFile> GenerateAsync(
        int sellerId,
        string source,
        string type,
        DateTime? rangeStartUtc,
        DateTime? rangeEndUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedSource = source.Trim().ToLowerInvariant();
        var normalizedType = type.Trim().ToLowerInvariant();

        var (fileNamePart, header, rows) = normalizedSource switch
        {
            ReportSource.Listings => await BuildListingsAsync(sellerId, normalizedType, cancellationToken),
            ReportSource.Orders => await BuildOrdersAsync(sellerId, normalizedType, rangeStartUtc, rangeEndUtc, cancellationToken),
            _ => throw new AppProblemException(422, "UNSUPPORTED_REPORT_SOURCE", $"'{source}' is not a supported report source.")
        };

        var csv = BuildCsv(header, rows);
        var fileName = $"{normalizedSource}-{fileNamePart}-{clock.UtcNow:yyyyMMdd_HHmmss}.csv";
        var fileUrl = await SaveAsync(fileName, csv, cancellationToken);
        return new GeneratedReportFile(fileUrl, rows.Count);
    }

    private async Task<(string TypeSlug, string[] Header, List<string[]> Rows)> BuildListingsAsync(
        int sellerId, string type, CancellationToken cancellationToken)
    {
        var statuses = type switch
        {
            ListingReportType.AllActive => new[] { ProductStatus.Active },
            ListingReportType.OutOfStock => new[] { ProductStatus.OutOfStock },
            ListingReportType.Hidden => new[] { ProductStatus.Hidden },
            ListingReportType.AuctionEnded => new[] { ProductStatus.AuctionEnded },
            _ => throw new AppProblemException(422, "UNRECOGNIZED_REPORT_TYPE", $"'{type}' is not a recognized listings report type.")
        };

        var listings = await reports.GetListingsForExportAsync(sellerId, statuses, MaxRows, cancellationToken);
        var header = new[] { "ProductId", "Title", "Price", "Quantity", "Status", "CreatedAtUtc", "UpdatedAtUtc" };
        var rows = listings.Select(l => new[]
        {
            l.ProductId.ToString(CultureInfo.InvariantCulture),
            l.Title,
            l.Price.ToString("0.##", CultureInfo.InvariantCulture),
            l.Quantity.ToString(CultureInfo.InvariantCulture),
            l.Status,
            l.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            l.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture)
        }).ToList();
        return (type, header, rows);
    }

    private async Task<(string TypeSlug, string[] Header, List<string[]> Rows)> BuildOrdersAsync(
        int sellerId, string type, DateTime? rangeStartUtc, DateTime? rangeEndUtc, CancellationToken cancellationToken)
    {
        string[]? statusFilter = type switch
        {
            OrderReportType.AllOrders => null,
            OrderReportType.WaitingPayment => new[] { "WaitingPayment" },
            OrderReportType.Confirmed => new[] { "Confirmed" },
            OrderReportType.Shipped => new[] { "Shipped" },
            OrderReportType.Delivered => new[] { "Delivered" },
            OrderReportType.Cancelled => new[] { "Cancelled" },
            _ => throw new AppProblemException(422, "UNRECOGNIZED_REPORT_TYPE", $"'{type}' is not a recognized orders report type.")
        };

        var now = clock.UtcNow;
        var toUtc = rangeEndUtc ?? now;
        var fromUtc = rangeStartUtc ?? toUtc.AddDays(-DefaultOrderRangeDays);
        if (toUtc < fromUtc) (fromUtc, toUtc) = (toUtc, fromUtc);

        var orders = await reports.GetOrdersForExportAsync(sellerId, fromUtc, toUtc, statusFilter, MaxRows, cancellationToken);
        var header = new[] { "OrderId", "Status", "Buyer", "Email", "Total", "OrderedAt", "PaidAt", "ShippedAt", "ItemCount", "Items" };
        var rows = orders.Select(o => new[]
        {
            o.OrderId.ToString(CultureInfo.InvariantCulture),
            o.Status,
            o.BuyerUsername,
            o.BuyerEmail,
            o.Total.ToString("0.##", CultureInfo.InvariantCulture),
            o.OrderedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            o.PaidAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            o.ShippedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            o.ItemCount.ToString(CultureInfo.InvariantCulture),
            o.Items
        }).ToList();
        return (type, header, rows);
    }

    private static byte[] BuildCsv(string[] header, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', header.Select(Escape)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',', row.Select(Escape)));
        }
        // UTF-8 with BOM so Excel opens Vietnamese text correctly.
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny(['"', ',', '\n', '\r']) < 0) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static async Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        // Same physical location LocalProductImageStorage writes to (API's wwwroot),
        // computed without an ASP.NET dependency since this class lives in Infrastructure.
        var directory = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "reports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
        return $"/uploads/reports/{fileName}";
    }
}
