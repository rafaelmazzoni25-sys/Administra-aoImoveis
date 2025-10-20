using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryNegotiationRepository : INegotiationRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryNegotiationRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(Negotiation negotiation, CancellationToken cancellationToken = default)
    {
        _store.Negotiations[negotiation.Id] = negotiation;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Negotiation>> GetActiveByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var result = _store.Negotiations.Values
            .Where(n => n.PropertyId == propertyId && n.PropertyBlocked)
            .ToList();
        return Task.FromResult<IReadOnlyList<Negotiation>>(result);
    }

    public Task<Negotiation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Negotiations.TryGetValue(id, out var negotiation);
        return Task.FromResult<Negotiation?>(negotiation);
    }

    public Task<IReadOnlyList<Negotiation>> GetExpiringProposalsAsync(DateTime referenceDate, CancellationToken cancellationToken = default)
    {
        var result = _store.Negotiations.Values
            .Where(n => n.ProposalExpiresAt.HasValue && n.ProposalExpiresAt.Value <= referenceDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<Negotiation>>(result);
    }

    public Task UpdateAsync(Negotiation negotiation, CancellationToken cancellationToken = default)
    {
        _store.Negotiations[negotiation.Id] = negotiation;
        return Task.CompletedTask;
    }
}
