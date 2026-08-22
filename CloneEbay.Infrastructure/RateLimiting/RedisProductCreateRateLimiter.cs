using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Products;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace CloneEbay.Infrastructure.RateLimiting;

public sealed class RedisProductCreateRateLimiter(
    IConnectionMultiplexer connection,
    IConfiguration configuration,
    IClock clock) : IProductCreateRateLimiter
{
    private const string Script = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        local ttl = redis.call('TTL', KEYS[1])
        return { count, ttl }
        """;

    private readonly int _limit = int.TryParse(configuration["RateLimiting:ProductCreatesPerHour"], out var configuredLimit)
        ? configuredLimit
        : 20;

    public async Task<ProductCreateQuotaResult> AcquireAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = clock.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var windowEnd = windowStart.AddHours(1);
        var expirySeconds = Math.Max(60, (int)Math.Ceiling((windowEnd - now).TotalSeconds) + 60);
        var key = $"cloneebay:rate-limit:product-create:{sellerId}:{windowStart:yyyyMMddHH}";
        var result = (RedisResult[]?)await connection.GetDatabase().ScriptEvaluateAsync(
            Script,
            [new RedisKey(key)],
            [(RedisValue)expirySeconds]) ?? [];
        var count = result.Length > 0 ? (long)result[0] : _limit + 1L;
        var ttl = result.Length > 1 ? Math.Max(1L, (long)result[1]) : expirySeconds;
        return new ProductCreateQuotaResult(
            count <= _limit,
            Math.Max(0, _limit - (int)Math.Min(count, int.MaxValue)),
            TimeSpan.FromSeconds(ttl));
    }
}
