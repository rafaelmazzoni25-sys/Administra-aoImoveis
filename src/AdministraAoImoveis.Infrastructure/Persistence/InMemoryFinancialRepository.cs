using System.Linq;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryFinancialRepository : IFinancialRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryFinancialRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(FinancialEntry entry, CancellationToken cancellationToken = default)
    {
        _store.FinancialEntries[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task<FinancialEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.FinancialEntries.TryGetValue(id, out var entry);
        return Task.FromResult<FinancialEntry?>(entry);
    }

    public Task<IReadOnlyCollection<FinancialEntry>> GetBlockingAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<FinancialEntry> result = _store.FinancialEntries.Values
            .Where(e => e.BlocksAvailability && e.Status is not (FinancialEntryStatus.Received or FinancialEntryStatus.Cancelled))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<FinancialEntry>> GetBlockingByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<FinancialEntry> result = _store.FinancialEntries.Values
            .Where(e => e.ReferenceId == propertyId && e.BlocksAvailability)
            .ToList();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(FinancialEntry entry, CancellationToken cancellationToken = default)
    {
        _store.FinancialEntries[entry.Id] = entry;
        return Task.CompletedTask;
    }
}
