using System.Collections.Concurrent;
using CloneEbay.Application.Dashboard;

namespace CloneEbay.Infrastructure.Caching;

public sealed class InMemoryDashboardCacheService : IDashboardCacheService
{
    private sealed record CacheEntry(object Value, DateTime ExpiresAtUtc);
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAtUtc > DateTime.UtcNow)
            {
                return Task.FromResult((T?)entry.Value);
            }
            _store.TryRemove(key, out _);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (value is not null)
        {
            _store[key] = new CacheEntry(value, DateTime.UtcNow.Add(ttl));
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}