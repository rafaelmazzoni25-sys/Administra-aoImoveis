using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryInspectionRepository : IInspectionRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryInspectionRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(Inspection inspection, CancellationToken cancellationToken = default)
    {
        _store.Inspections[inspection.Id] = inspection;
        return Task.CompletedTask;
    }

    public Task<Inspection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Inspections.TryGetValue(id, out var inspection);
        return Task.FromResult<Inspection?>(inspection);
    }

    public Task<IReadOnlyList<Inspection>> GetScheduledForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var result = _store.Inspections.Values
            .Where(i => i.PropertyId == propertyId)
            .ToList();
        return Task.FromResult<IReadOnlyList<Inspection>>(result);
    }

    public Task UpdateAsync(Inspection inspection, CancellationToken cancellationToken = default)
    {
        _store.Inspections[inspection.Id] = inspection;
        return Task.CompletedTask;
    }
}
