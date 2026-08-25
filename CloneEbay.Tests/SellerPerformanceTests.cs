using CloneEbay.Application.Sellers;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Tests;

public sealed class SellerPerformanceTests
{
    [Fact]
    public void DetermineLevel_meeting_all_four_top_rated_thresholds_returns_TopRated()
    {
        var level = SellerPerformanceCalculator.DetermineLevel(
            defectRate: 0.4m, lateShipRate: 1.5m, trackingRate: 96m, casesClosedRate: 0.1m);

        Assert.Equal(SellerPerformanceLevel.TopRated, level);
    }

    [Fact]
    public void DetermineLevel_failing_just_one_top_rated_threshold_falls_back_to_AboveStandard()
    {
        // Tracking rate 92% clears AboveStandard (>=90) but misses TopRated (>=95).
        var level = SellerPerformanceCalculator.DetermineLevel(
            defectRate: 0.4m, lateShipRate: 1.5m, trackingRate: 92m, casesClosedRate: 0.1m);

        Assert.Equal(SellerPerformanceLevel.AboveStandard, level);
    }

    [Fact]
    public void DetermineLevel_failing_above_standard_falls_back_to_BelowStandard()
    {
        var level = SellerPerformanceCalculator.DetermineLevel(
            defectRate: 5m, lateShipRate: 1m, trackingRate: 99m, casesClosedRate: 0.1m);

        Assert.Equal(SellerPerformanceLevel.BelowStandard, level);
    }

    [Fact]
    public void ApplyMinimumRequirements_TopRated_without_enough_volume_downgrades_to_AboveStandard()
    {
        var level = SellerPerformanceCalculator.ApplyMinimumRequirements(
            SellerPerformanceLevel.TopRated, transactionsLast12Months: 50, salesLast12Months: 200m);

        Assert.Equal(SellerPerformanceLevel.AboveStandard, level);
    }

    [Theory]
    [InlineData(100, 1000)] // option 1: >=100 transactions AND >=$1000 sales
    [InlineData(400, 0)]    // option 2: >=400 transactions regardless of sales
    public void ApplyMinimumRequirements_TopRated_meeting_either_option_stays_TopRated(int transactions, decimal sales)
    {
        var level = SellerPerformanceCalculator.ApplyMinimumRequirements(
            SellerPerformanceLevel.TopRated, transactions, sales);

        Assert.Equal(SellerPerformanceLevel.TopRated, level);
    }

    [Fact]
    public void ApplyMinimumRequirements_does_not_touch_non_TopRated_levels()
    {
        var level = SellerPerformanceCalculator.ApplyMinimumRequirements(
            SellerPerformanceLevel.AboveStandard, transactionsLast12Months: 0, salesLast12Months: 0m);

        Assert.Equal(SellerPerformanceLevel.AboveStandard, level);
    }

    [Fact]
    public void GetWarnings_returns_Critical_overall_when_any_metric_is_critical()
    {
        var warnings = SellerPerformanceCalculator.GetWarnings(
            defectRate: 1.9m, lateShipRate: 1m, trackingRate: 99m, casesClosedRate: 0.1m);

        Assert.Equal("Critical", SellerPerformanceCalculator.OverallSeverity(warnings));
        Assert.Contains(warnings, w => w.Metric == "TransactionDefectRate" && w.Severity == "Critical");
    }

    [Fact]
    public void GetWarnings_returns_null_severity_when_metrics_are_healthy()
    {
        var warnings = SellerPerformanceCalculator.GetWarnings(
            defectRate: 0.2m, lateShipRate: 1m, trackingRate: 99m, casesClosedRate: 0.1m);

        Assert.Empty(warnings);
        Assert.Null(SellerPerformanceCalculator.OverallSeverity(warnings));
    }

    // Spec caps are stated in USD; this codebase prices everything in VND, so
    // SellingLimitPolicy scales them by an approximate 25,000 VND/USD rate.
    [Theory]
    [InlineData(SellerPerformanceLevel.BelowStandard, 10, 12_500_000)]
    [InlineData(SellerPerformanceLevel.AboveStandard, 100, 125_000_000)]
    [InlineData(SellerPerformanceLevel.TopRated, 1000, 1_250_000_000)]
    public void SellingLimitPolicy_limits_match_spec_table_scaled_to_vnd(SellerPerformanceLevel level, int expectedCount, decimal expectedValue)
    {
        var (maxCount, maxValue) = SellingLimitPolicy.GetLimits(level);
        Assert.Equal(expectedCount, maxCount);
        Assert.Equal(expectedValue, maxValue);
    }

    [Fact]
    public void SellingLimitPolicy_touching_the_count_limit_exactly_is_not_allowed()
    {
        // BelowStandard max is 10 listings; already at 10 => strictly "<" fails.
        var canList = SellingLimitPolicy.CanList(SellerPerformanceLevel.BelowStandard, currentActiveListingCount: 10, currentActiveTotalValue: 0m, newListingEstimatedValue: 1m);
        Assert.False(canList);
    }

    [Fact]
    public void SellingLimitPolicy_touching_the_value_limit_exactly_is_not_allowed()
    {
        // BelowStandard max value is 12,500,000 VND; 12,499,999 + 1 == limit => strictly "<" fails.
        var canList = SellingLimitPolicy.CanList(SellerPerformanceLevel.BelowStandard, currentActiveListingCount: 0, currentActiveTotalValue: 12_499_999m, newListingEstimatedValue: 1m);
        Assert.False(canList);
    }

    [Fact]
    public void SellingLimitPolicy_allows_listing_when_strictly_under_both_limits()
    {
        var canList = SellingLimitPolicy.CanList(SellerPerformanceLevel.BelowStandard, currentActiveListingCount: 9, currentActiveTotalValue: 3_000_000m, newListingEstimatedValue: 2_000_000m);
        Assert.True(canList);
    }

    [Fact]
    public void EvaluationSchedule_before_day_20_targets_day_20_this_month()
    {
        var now = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);
        var next = SellerPerformanceEvaluationSchedule.NextEvaluationDate(now);
        Assert.Equal(new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void EvaluationSchedule_on_or_after_day_20_targets_day_20_next_month()
    {
        var now = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var next = SellerPerformanceEvaluationSchedule.NextEvaluationDate(now);
        Assert.Equal(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void EvaluationSchedule_is_only_due_on_day_20()
    {
        Assert.True(SellerPerformanceEvaluationSchedule.IsEvaluationDue(new DateTime(2026, 8, 20, 3, 0, 0, DateTimeKind.Utc)));
        Assert.False(SellerPerformanceEvaluationSchedule.IsEvaluationDue(new DateTime(2026, 8, 19, 23, 59, 0, DateTimeKind.Utc)));
    }
}
