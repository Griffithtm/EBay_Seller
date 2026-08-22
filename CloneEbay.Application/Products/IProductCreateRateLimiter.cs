namespace CloneEbay.Application.Products;

public sealed record ProductCreateQuotaResult(bool IsAllowed, int Remaining, TimeSpan RetryAfter);

public interface IProductCreateRateLimiter
{
    Task<ProductCreateQuotaResult> AcquireAsync(int sellerId, CancellationToken cancellationToken = default);
}
