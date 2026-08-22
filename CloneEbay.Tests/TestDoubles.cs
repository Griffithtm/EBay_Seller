using CloneEbay.Application.Abstractions.Persistence;

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
