using CloneEbay.Application.Payments;

namespace CloneEbay.Tests;

public sealed class PaymentReportPeriodResolverTests
{
    private static readonly DateTime Now = new(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Resolve_thisMonth_spans_the_full_calendar_month()
    {
        var (from, to) = PaymentReportPeriodResolver.Resolve(PaymentReportPeriod.ThisMonth, Now);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), from);
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1), to);
    }

    [Fact]
    public void Resolve_lastMonth_spans_the_previous_calendar_month()
    {
        var (from, to) = PaymentReportPeriodResolver.Resolve(PaymentReportPeriod.LastMonth, Now);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), from);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1), to);
    }

    [Fact]
    public void Resolve_last90Days_ends_now_and_starts_89_days_before()
    {
        var (from, to) = PaymentReportPeriodResolver.Resolve(PaymentReportPeriod.Last90Days, Now);
        Assert.Equal(Now.Date.AddDays(-89), from);
        Assert.Equal(Now, to);
    }

    [Fact]
    public void Resolve_thisYear_spans_the_full_calendar_year()
    {
        var (from, to) = PaymentReportPeriodResolver.Resolve(PaymentReportPeriod.ThisYear, Now);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), from);
        Assert.Equal(2026, to.Year);
        Assert.Equal(12, to.Month);
        Assert.Equal(31, to.Day);
    }

    [Fact]
    public void ResolveComparison_previousPeriod_is_immediately_before_and_same_length()
    {
        var (curFrom, curTo) = PaymentReportPeriodResolver.Resolve(PaymentReportPeriod.ThisMonth, Now);
        var (compFrom, compTo) = PaymentReportPeriodResolver.ResolveComparison(PaymentReportCompared.PreviousPeriod, curFrom, curTo);

        Assert.Equal(curTo - curFrom, compTo - compFrom);
        Assert.True(compTo < curFrom);
    }

    [Fact]
    public void ResolveComparison_samePeriodLastYear_shifts_both_bounds_by_one_year()
    {
        var (curFrom, curTo) = PaymentReportPeriodResolver.Resolve(PaymentReportPeriod.ThisMonth, Now);
        var (compFrom, compTo) = PaymentReportPeriodResolver.ResolveComparison(PaymentReportCompared.SamePeriodLastYear, curFrom, curTo);

        Assert.Equal(curFrom.AddYears(-1), compFrom);
        Assert.Equal(curTo.AddYears(-1), compTo);
    }

    [Theory]
    [InlineData("thisMonth", true)]
    [InlineData("THISMONTH", true)]
    [InlineData("nextMonth", false)]
    public void ValidPeriods_whitelist_is_case_insensitive_and_rejects_unknown_values(string period, bool expected)
    {
        Assert.Equal(expected, PaymentReportPeriodResolver.ValidPeriods.Contains(period));
    }

    [Fact]
    public void RoundMoney_rounds_away_from_zero_to_two_decimals()
    {
        Assert.Equal(1.24m, PaymentReportPeriodResolver.RoundMoney(1.235m));
        Assert.Equal(-1.24m, PaymentReportPeriodResolver.RoundMoney(-1.235m));
    }
}
