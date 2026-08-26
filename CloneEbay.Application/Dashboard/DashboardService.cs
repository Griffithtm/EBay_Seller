using System.Globalization;
using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Sellers;

namespace CloneEbay.Application.Dashboard;

public sealed class DashboardService(
    IDashboardRepository repository,
    IDashboardCacheService cache,
    IClock clock,
    ISellerPerformanceService sellerPerformance) : IDashboardService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public async Task<DashboardSummaryDto> GetSummaryAsync(int sellerId, string? period, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = NormalizePeriod(period);
        var cacheKey = $"seller:{sellerId}:dashboard:summary:{normalizedPeriod}";
        var cached = await cache.GetAsync<DashboardSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var (fromUtc, toUtc) = GetDateRange(normalizedPeriod, clock.UtcNow);
        var (totalRevenue, totalOrders, aov) = await repository.GetSummaryAsync(sellerId, fromUtc, toUtc, cancellationToken);
        var result = new DashboardSummaryDto(totalRevenue, totalOrders, aov, normalizedPeriod, fromUtc, toUtc);

        await cache.SetAsync(cacheKey, result, DefaultTtl, cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int sellerId, string? period, int limit = 10, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = NormalizePeriod(period);
        var clampedLimit = Math.Clamp(limit, 1, 50);
        var cacheKey = $"seller:{sellerId}:dashboard:top:{normalizedPeriod}:{clampedLimit}";
        var cached = await cache.GetAsync<IReadOnlyList<TopProductDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var (fromUtc, toUtc) = GetDateRange(normalizedPeriod, clock.UtcNow);
        var result = await repository.GetTopProductsAsync(sellerId, fromUtc, toUtc, clampedLimit, cancellationToken);

        await cache.SetAsync(cacheKey, result, DefaultTtl, cancellationToken);
        return result;
    }

    public async Task<RevenueChartDto> GetRevenueChartAsync(int sellerId, string? period, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = NormalizePeriod(period);
        var cacheKey = $"seller:{sellerId}:dashboard:chart:{normalizedPeriod}";
        var cached = await cache.GetAsync<RevenueChartDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var now = clock.UtcNow;
        var (fromUtc, toUtc) = GetDateRange(normalizedPeriod, now);
        var aggregates = await repository.GetDailyAggregatesAsync(sellerId, fromUtc, toUtc, cancellationToken);

        var points = BuildChartPoints(normalizedPeriod, fromUtc, toUtc, aggregates);
        var totalRevenue = points.Sum(p => p.Revenue);
        var totalOrders = points.Sum(p => p.OrderCount);
        var result = new RevenueChartDto(normalizedPeriod, totalRevenue, totalOrders, points);

        await cache.SetAsync(cacheKey, result, DefaultTtl, cancellationToken);
        return result;
    }

    public async Task<SellerPerformanceDto> GetPerformanceAsync(int sellerId, string? period, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = NormalizePeriod(period);
        var cacheKey = $"seller:{sellerId}:dashboard:perf:{normalizedPeriod}";
        var cached = await cache.GetAsync<SellerPerformanceDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var (fromUtc, toUtc) = GetDateRange(normalizedPeriod, clock.UtcNow);
        var (totalOrders, lateShipmentRate, disputeRate, positiveFeedbackRate) =
            await repository.GetPerformanceAsync(sellerId, fromUtc, toUtc, cancellationToken);

        // SellerLevel is NOT scoped to this week/month window — it is the persisted,
        // rolling-12-month level from the real Seller Performance evaluation (spec Part A),
        // shown here alongside this period's own fulfilment figures for context.
        var level = (await sellerPerformance.GetAsync(sellerId, cancellationToken)).Level;

        var result = new SellerPerformanceDto(normalizedPeriod, totalOrders, lateShipmentRate, disputeRate, level, positiveFeedbackRate);

        await cache.SetAsync(cacheKey, result, DefaultTtl, cancellationToken);
        return result;
    }

    private static string NormalizePeriod(string? period) =>
        string.Equals(period, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";

    public static (DateTime FromUtc, DateTime ToUtc) GetDateRange(string normalizedPeriod, DateTime nowUtc)
    {
        if (normalizedPeriod == "month")
        {
            var from = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddMonths(1);
            return (from, to);
        }
        else
        {
            // Default "week": Current week from Monday to next Monday
            var diff = (7 + (int)nowUtc.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var monday = nowUtc.Date.AddDays(-diff);
            var from = DateTime.SpecifyKind(monday, DateTimeKind.Utc);
            var to = from.AddDays(7);
            return (from, to);
        }
    }

    private static IReadOnlyList<RevenueChartPointDto> BuildChartPoints(
        string normalizedPeriod,
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyList<DailyOrderAggregateDto> aggregates)
    {
        var dict = aggregates.ToDictionary(a => a.DateUtc.Date, a => a);
        var points = new List<RevenueChartPointDto>();

        if (normalizedPeriod == "week")
        {
            var dayLabels = new[] { "T2 (Thứ 2)", "T3 (Thứ 3)", "T4 (Thứ 4)", "T5 (Thứ 5)", "T6 (Thứ 6)", "T7 (Thứ 7)", "CN (Chủ nhật)" };
            for (var i = 0; i < 7; i++)
            {
                var currentDay = fromUtc.Date.AddDays(i);
                dict.TryGetValue(currentDay, out var agg);
                points.Add(new RevenueChartPointDto(
                    dayLabels[i],
                    currentDay.ToString("yyyy-MM-dd"),
                    agg?.Revenue ?? 0m,
                    agg?.OrderCount ?? 0));
            }
        }
        else
        {
            // Monthly: 4-5 weeks bucket
            var current = fromUtc.Date;
            var weekIndex = 1;
            while (current < toUtc.Date)
            {
                var weekEnd = current.AddDays(7) < toUtc.Date ? current.AddDays(7) : toUtc.Date;
                var weekAggregates = aggregates.Where(a => a.DateUtc.Date >= current && a.DateUtc.Date < weekEnd).ToArray();
                var weekRevenue = weekAggregates.Sum(a => a.Revenue);
                var weekOrderCount = weekAggregates.Sum(a => a.OrderCount);

                points.Add(new RevenueChartPointDto(
                    $"Tuần {weekIndex} ({current:dd/MM} - {weekEnd.AddDays(-1):dd/MM})",
                    current.ToString("yyyy-MM-dd"),
                    weekRevenue,
                    weekOrderCount));

                current = weekEnd;
                weekIndex++;
            }
        }

        return points;
    }
}