using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryCommunicationRepository : ICommunicationRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryCommunicationRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(CommunicationThread thread, CancellationToken cancellationToken = default)
    {
        _store.CommunicationThreads[thread.Id] = thread;
        return Task.CompletedTask;
    }

    public Task<CommunicationThread?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.CommunicationThreads.TryGetValue(id, out var thread);
        return Task.FromResult<CommunicationThread?>(thread);
    }

    public Task UpdateAsync(CommunicationThread thread, CancellationToken cancellationToken = default)
    {
        _store.CommunicationThreads[thread.Id] = thread;
        return Task.CompletedTask;
    }
}
