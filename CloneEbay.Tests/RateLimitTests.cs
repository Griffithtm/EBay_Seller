using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Infrastructure.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace CloneEbay.Tests;

public sealed class RateLimitTests
{
    [Fact]
    public async Task Product_create_quota_is_partitioned_by_seller()
    {
        var configuration = new TestConfiguration("2");
        var limiter = new InMemoryProductCreateRateLimiter(configuration, new FixedClock());

        Assert.True((await limiter.AcquireAsync(10)).IsAllowed);
        Assert.True((await limiter.AcquireAsync(10)).IsAllowed);
        var rejected = await limiter.AcquireAsync(10);
        Assert.False(rejected.IsAllowed);
        Assert.True(rejected.RetryAfter > TimeSpan.Zero);
        Assert.True((await limiter.AcquireAsync(11)).IsAllowed);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 23, 12, 30, 0, DateTimeKind.Utc);
    }

    private sealed class TestConfiguration(string limit) : IConfiguration
    {
        public string? this[string key]
        {
            get => key == "RateLimiting:ProductCreatesPerHour" ? limit : null;
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];
        public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);
        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }
}
