using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Infrastructure.FileStorage;

public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(string fileName, string contentType, Stream content, string category, CancellationToken cancellationToken = default);
    Task<Stream> OpenAsync(StoredFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(StoredFile file, CancellationToken cancellationToken = default);
}
