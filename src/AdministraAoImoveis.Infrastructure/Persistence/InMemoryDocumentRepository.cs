using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryDocumentRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(DocumentRecord document, CancellationToken cancellationToken = default)
    {
        _store.Documents[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task<DocumentRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Documents.TryGetValue(id, out var document);
        return Task.FromResult<DocumentRecord?>(document);
    }

    public Task<IReadOnlyList<DocumentRecord>> GetByOwnerAsync(Guid ownerId, string ownerType, CancellationToken cancellationToken = default)
    {
        var result = _store.Documents.Values
            .Where(d => d.OwnerId == ownerId && string.Equals(d.OwnerType, ownerType, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<DocumentRecord>>(result);
    }
}
