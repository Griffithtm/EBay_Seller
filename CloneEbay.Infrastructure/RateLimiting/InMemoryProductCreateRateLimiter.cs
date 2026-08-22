using System.Collections.Concurrent;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Products;
using Microsoft.Extensions.Configuration;

namespace CloneEbay.Infrastructure.RateLimiting;

public sealed class InMemoryProductCreateRateLimiter(IConfiguration configuration, IClock clock)
    : IProductCreateRateLimiter
{
    private readonly ConcurrentDictionary<int, WindowCounter> _counters = new();
    private readonly int _limit = int.TryParse(configuration["RateLimiting:ProductCreatesPerHour"], out var configuredLimit)
        ? configuredLimit
        : 20;

    public Task<ProductCreateQuotaResult> AcquireAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = clock.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var windowEnd = windowStart.AddHours(1);
        var counter = _counters.GetOrAdd(sellerId, _ => new WindowCounter(windowStart));
        lock (counter)
        {
            if (counter.WindowStart != windowStart)
            {
                counter.WindowStart = windowStart;
                counter.Count = 0;
            }
            counter.Count++;
            var allowed = counter.Count <= _limit;
            return Task.FromResult(new ProductCreateQuotaResult(
                allowed,
                Math.Max(0, _limit - counter.Count),
                windowEnd - now));
        }
    }

    private sealed class WindowCounter(DateTime windowStart)
    {
        public DateTime WindowStart { get; set; } = windowStart;
        public int Count { get; set; }
    }
}
