namespace CloneEbay.API.Storage;

public interface IProductImageStorage
{
    Task<IReadOnlyList<string>> SaveManyAsync(IReadOnlyList<IFormFile> files, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IReadOnlyList<string> imageUrls, CancellationToken cancellationToken = default);
}
