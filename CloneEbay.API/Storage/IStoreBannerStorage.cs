namespace CloneEbay.API.Storage;

public interface IStoreBannerStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}
