using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;

namespace CloneEbay.Tests;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task<IAppTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IAppTransaction>(new FakeTransaction());

    private sealed class FakeTransaction : IAppTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

internal sealed class FixedClock(DateTime? fixedUtc = null) : IClock
{
    public DateTime UtcNow => fixedUtc ?? new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
}
