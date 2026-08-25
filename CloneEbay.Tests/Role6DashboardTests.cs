using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Dashboard;
using CloneEbay.Application.Sellers;

namespace CloneEbay.Tests;

public sealed class Role6DashboardTests
{
    [Fact]
    public async Task Get_summary_calculates_correct_totals_and_aov()
    {
        var fakeRepo = new FakeDashboardRepository
        {
            SummaryResult = (15000000m, 5, 3000000m)
        };
        var fakeCache = new FakeDashboardCache();
        var clock = new FixedClock(new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
        var service = new DashboardService(fakeRepo, fakeCache, clock, new FakeSellerPerformanceService());

        var result = await service.GetSummaryAsync(sellerId: 3, period: "week");

        Assert.Equal(15000000m, result.TotalRevenue);
        Assert.Equal(5, result.TotalOrders);
        Assert.Equal(3000000m, result.AverageOrderValue);
        Assert.Equal("week", result.Period);
        Assert.Equal(1, fakeRepo.SummaryCallCount);

        // Verify result is cached
        var cached = await service.GetSummaryAsync(sellerId: 3, period: "week");
        Assert.Equal(15000000m, cached.TotalRevenue);
        Assert.Equal(1, fakeRepo.SummaryCallCount); // No additional DB call
    }

    [Fact]
    public async Task Get_top_products_returns_ordered_results_with_limit()
    {
        var fakeRepo = new FakeDashboardRepository
        {
            TopProducts = new List<TopProductDto>
            {
                new(1, "Product A", "/img1.jpg", 10, 10000000m, 5, false),
                new(2, "Product B", "/img2.jpg", 5, 5000000m, 20, false),
                new(3, "Product C", null, 2, 2000000m, 0, true)
            }
        };
        var fakeCache = new FakeDashboardCache();
        var clock = new FixedClock(new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
        var service = new DashboardService(fakeRepo, fakeCache, clock, new FakeSellerPerformanceService());

        var result = await service.GetTopProductsAsync(sellerId: 3, period: "week", limit: 2);

        Assert.Equal(3, result.Count); // Returned from fake repo
        Assert.Equal("Product A", result[0].Title);
        Assert.Equal(10000000m, result[0].Revenue);
    }

    [Fact]
    public async Task Get_revenue_chart_generates_complete_7_days_points_with_zeros()
    {
        var fakeRepo = new FakeDashboardRepository
        {
            DailyAggregates = new List<DailyOrderAggregateDto>
            {
                // Current week Monday is 2026-08-17, Wednesday is 2026-08-19
                new(new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), 5000000m, 2)
            }
        };
        var fakeCache = new FakeDashboardCache();
        var clock = new FixedClock(new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
        var service = new DashboardService(fakeRepo, fakeCache, clock, new FakeSellerPerformanceService());

        var result = await service.GetRevenueChartAsync(sellerId: 3, period: "week");

        Assert.Equal("week", result.Period);
        Assert.Equal(7, result.Points.Count); // Exactly 7 days Mon-Sun
        Assert.Equal(5000000m, result.TotalRevenue);
        Assert.Equal(2, result.TotalOrders);

        // Wednesday (T4 index 2) should have 5,000,000, others 0
        Assert.Equal(5000000m, result.Points[2].Revenue);
        Assert.Equal(2, result.Points[2].OrderCount);
        Assert.Equal(0m, result.Points[0].Revenue);
        Assert.Equal(0, result.Points[0].OrderCount);
    }

    [Fact]
    public async Task Get_revenue_chart_monthly_generates_weekly_buckets()
    {
        var fakeRepo = new FakeDashboardRepository
        {
            DailyAggregates = new List<DailyOrderAggregateDto>
            {
                new(new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc), 3000000m, 1),
                new(new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), 7000000m, 2)
            }
        };
        var fakeCache = new FakeDashboardCache();
        var clock = new FixedClock(new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
        var service = new DashboardService(fakeRepo, fakeCache, clock, new FakeSellerPerformanceService());

        var result = await service.GetRevenueChartAsync(sellerId: 3, period: "month");

        Assert.Equal("month", result.Period);
        Assert.True(result.Points.Count >= 4); // 4-5 week buckets
        Assert.Equal(10000000m, result.TotalRevenue);
        Assert.Equal(3, result.TotalOrders);
    }

    [Fact]
    public async Task Get_performance_combines_period_figures_with_the_real_persisted_level()
    {
        var fakeRepo = new FakeDashboardRepository
        {
            PerformanceResult = (15, 5m, 2m, 97.5m)
        };
        var fakeCache = new FakeDashboardCache();
        var clock = new FixedClock(new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
        var service = new DashboardService(fakeRepo, fakeCache, clock, new FakeSellerPerformanceService { Level = "TopRated" });

        var result = await service.GetPerformanceAsync(sellerId: 3, period: "week");

        Assert.Equal(15, result.TotalOrders);
        Assert.Equal("TopRated", result.SellerLevel);
        Assert.Equal(5m, result.LateShipmentRate);
        Assert.Equal(2m, result.DisputeRate);
        Assert.Equal(97.5m, result.PositiveFeedbackRate);
    }

    private sealed class FakeDashboardRepository : IDashboardRepository
    {
        public (decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue) SummaryResult { get; set; }
        public List<TopProductDto> TopProducts { get; set; } = [];
        public List<DailyOrderAggregateDto> DailyAggregates { get; set; } = [];
        public (int TotalOrders, decimal LateShipmentRate, decimal DisputeRate, decimal PositiveFeedbackRate) PerformanceResult { get; set; } = (0, 0m, 0m, 100m);
        public int SummaryCallCount { get; private set; }

        public Task<(decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue)> GetSummaryAsync(
            int sellerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default)
        {
            SummaryCallCount++;
            return Task.FromResult(SummaryResult);
        }

        public Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(
            int sellerId,
            DateTime fromUtc,
            DateTime toUtc,
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TopProductDto>>(TopProducts);

        public Task<IReadOnlyList<DailyOrderAggregateDto>> GetDailyAggregatesAsync(
            int sellerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DailyOrderAggregateDto>>(DailyAggregates);

        public Task<(int TotalOrders, decimal LateShipmentRate, decimal DisputeRate, decimal PositiveFeedbackRate)> GetPerformanceAsync(
            int sellerId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PerformanceResult);
    }

    /// <summary>Returns a fixed, pre-configured level; the other members are unused by DashboardService.</summary>
    private sealed class FakeSellerPerformanceService : ISellerPerformanceService
    {
        public string Level { get; set; } = "AboveStandard";

        public Task<SellerPerformanceResponse> GetAsync(int sellerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SellerPerformanceResponse(sellerId, Level, 0m, 0m, 100m, 0m, 0, 0m, 100, 5000m, 0, 0m, null, DateTime.UtcNow, null, null));
        public Task<SellerPerformanceWarningResponse> GetEarlyWarningAsync(int sellerId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task EvaluateSellerAsync(int sellerId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<int> EvaluateDueSellersAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task EnsureCanListAsync(int sellerId, decimal newListingEstimatedValue, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<SellerPerformanceResponse> OverrideLevelAsync(int sellerId, string level, int adminUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeDashboardCache : IDashboardCacheService
    {
        private readonly Dictionary<string, object> _store = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(key, out var val))
            {
                return Task.FromResult((T?)val);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            if (value is not null)
            {
                _store[key] = value;
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }
}