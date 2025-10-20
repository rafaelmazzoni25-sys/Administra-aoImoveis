using System.Linq;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryLeadRepository : ILeadRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryLeadRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _store.Leads[lead.Id] = lead;
        return Task.CompletedTask;
    }

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Leads.TryGetValue(id, out var lead);
        return Task.FromResult<Lead?>(lead);
    }

    public Task<IReadOnlyCollection<Lead>> SearchAsync(string? assignedTo, CancellationToken cancellationToken = default)
    {
        var result = _store.Leads.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            result = result.Where(l => string.Equals(l.AssignedTo, assignedTo, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyCollection<Lead>>(result.ToList());
    }

    public Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _store.Leads[lead.Id] = lead;
        return Task.CompletedTask;
    }
}
