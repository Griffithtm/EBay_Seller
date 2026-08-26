namespace CloneEbay.Application.Payments;

public static class PaymentReportPeriod
{
    public const string ThisMonth = "thisMonth";
    public const string LastMonth = "lastMonth";
    public const string Last90Days = "last90Days";
    public const string ThisYear = "thisYear";
}

public static class PaymentReportCompared
{
    public const string None = "none";
    public const string PreviousPeriod = "previousPeriod";
    public const string SamePeriodLastYear = "samePeriodLastYear";
}

public sealed record PaymentReportFilter(string Period = PaymentReportPeriod.ThisMonth, string Compared = PaymentReportCompared.None);

public sealed record PaymentReportSectionDto(decimal GrossSales, decimal Refunds, decimal Expenses, decimal Charges, decimal Payouts, decimal NetTransfersTotal);

public sealed record PaymentReportPeriodDto(DateTime FromUtc, DateTime ToUtc, PaymentReportSectionDto Figures, string? Note);

public sealed record PaymentReportResponse(string Currency, PaymentReportPeriodDto Current, PaymentReportPeriodDto? Comparison);
