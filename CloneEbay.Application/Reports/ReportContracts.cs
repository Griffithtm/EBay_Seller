namespace CloneEbay.Application.Reports;

/// <summary>
/// Fixed lookup codes for Source/Type — used as the actual matching key everywhere
/// (spec A.5 checklist: "dùng label làm khoá tra cứu là kỹ thuật dễ vỡ" — this codebase
/// uses stable codes instead; a UI is free to show any localized label for these).
/// </summary>
public static class ReportSource
{
    public const string Listings = "listings";
    public const string Orders = "orders";
}

public static class ListingReportType
{
    public const string AllActive = "all-active";
    public const string OutOfStock = "out-of-stock";
    public const string Hidden = "hidden";
    public const string AuctionEnded = "auction-ended";
}

public static class OrderReportType
{
    public const string AllOrders = "all-orders";
    public const string WaitingPayment = "waiting-payment";
    public const string Confirmed = "confirmed";
    public const string Shipped = "shipped";
    public const string Delivered = "delivered";
    public const string Cancelled = "cancelled";
}

public sealed record CreateReportDownloadRequest(
    string Source,
    string Type,
    DateTime? RangeStartUtc,
    DateTime? RangeEndUtc,
    string? TimeZone);

public sealed record ReportDownloadResponse(
    int Id,
    string ReferenceCode,
    string Source,
    string Type,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc,
    string? FileUrl,
    DateTime? RangeStartUtc,
    DateTime? RangeEndUtc);

public sealed record ReportDownloadListQuery(
    string? Source,
    string? Status,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize);

public sealed record CreateReportScheduleRequest(
    string Source,
    string Type,
    string Frequency,
    string? DeliveryEmail,
    DateTime? EndDateUtc);

public sealed record ReportScheduleResponse(
    int Id,
    string Source,
    string Type,
    string Frequency,
    DateTime CreatedAtUtc,
    DateTime? LastRunAtUtc,
    DateTime? NextRunAtUtc,
    DateTime? EndDateUtc,
    bool IsActive,
    string? DeliveryEmail);

public sealed record ReportScheduleListQuery(
    string? Source,
    string? Type,
    string? Frequency,
    bool OnlyActive,
    int Page,
    int PageSize);
