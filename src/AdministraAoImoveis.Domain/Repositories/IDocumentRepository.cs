using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface IDocumentRepository
{
    Task<DocumentRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(DocumentRecord document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentRecord>> GetByOwnerAsync(Guid ownerId, string ownerType, CancellationToken cancellationToken = default);
}
