namespace CloneEbay.Application.Payments;

/// <summary>
/// Pure date-range arithmetic (spec B.2) — the one part of the Payments Report spec that is
/// data-independent and marked "đạt chuẩn, nên copy nguyên". No dependency on a clock instance
/// so it is trivially unit-testable; callers pass "now" explicitly.
/// </summary>
public static class PaymentReportPeriodResolver
{
    public static readonly IReadOnlySet<string> ValidPeriods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { PaymentReportPeriod.ThisMonth, PaymentReportPeriod.LastMonth, PaymentReportPeriod.Last90Days, PaymentReportPeriod.ThisYear };

    public static readonly IReadOnlySet<string> ValidCompared = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { PaymentReportCompared.None, PaymentReportCompared.PreviousPeriod, PaymentReportCompared.SamePeriodLastYear };

    public static (DateTime FromUtc, DateTime ToUtc) Resolve(string period, DateTime nowUtc)
    {
        return period.ToLowerInvariant() switch
        {
            var p when p.Equals(PaymentReportPeriod.ThisMonth, StringComparison.OrdinalIgnoreCase) =>
                MonthRange(nowUtc.Year, nowUtc.Month),
            var p when p.Equals(PaymentReportPeriod.LastMonth, StringComparison.OrdinalIgnoreCase) =>
                MonthRange(nowUtc.AddMonths(-1).Year, nowUtc.AddMonths(-1).Month),
            var p when p.Equals(PaymentReportPeriod.ThisYear, StringComparison.OrdinalIgnoreCase) =>
                (new DateTime(nowUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(nowUtc.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc)),
            // last90Days, and the unreachable-after-validation fallback (spec B.2).
            _ => (nowUtc.Date.AddDays(-89), nowUtc)
        };
    }

    public static (DateTime FromUtc, DateTime ToUtc) ResolveComparison(string compared, DateTime currentFromUtc, DateTime currentToUtc)
    {
        var span = currentToUtc - currentFromUtc;
        return compared.ToLowerInvariant() switch
        {
            var c when c.Equals(PaymentReportCompared.PreviousPeriod, StringComparison.OrdinalIgnoreCase) =>
                (currentFromUtc - span - TimeSpan.FromTicks(1), currentFromUtc - TimeSpan.FromTicks(1)),
            var c when c.Equals(PaymentReportCompared.SamePeriodLastYear, StringComparison.OrdinalIgnoreCase) =>
                (currentFromUtc.AddYears(-1), currentToUtc.AddYears(-1)),
            _ => (currentFromUtc, currentToUtc)
        };
    }

    private static (DateTime, DateTime) MonthRange(int year, int month)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1).AddTicks(-1);
        return (from, to);
    }

    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
