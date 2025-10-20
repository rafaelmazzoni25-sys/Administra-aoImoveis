using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryPropertyRepository : IPropertyRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryPropertyRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(Property property, CancellationToken cancellationToken = default)
    {
        _store.Properties[property.Id] = property;
        return Task.CompletedTask;
    }

    public Task<Property?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var property = _store.Properties.Values.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<Property?>(property);
    }

    public Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Properties.TryGetValue(id, out var property);
        return Task.FromResult<Property?>(property);
    }

    public Task<IReadOnlyList<Property>> SearchAvailableAsync(DateTime referenceDate, CancellationToken cancellationToken = default)
    {
        var result = _store.Properties.Values
            .Where(p => p.Status == PropertyOperationalStatus.Disponivel || (p.AvailableFrom.HasValue && p.AvailableFrom.Value <= referenceDate))
            .ToList();
        return Task.FromResult<IReadOnlyList<Property>>(result);
    }

    public Task UpdateAsync(Property property, CancellationToken cancellationToken = default)
    {
        _store.Properties[property.Id] = property;
        return Task.CompletedTask;
    }
}
