using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface INegotiationRepository
{
    Task<Negotiation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Negotiation>> GetActiveByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task AddAsync(Negotiation negotiation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Negotiation negotiation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Negotiation>> GetExpiringProposalsAsync(DateTime referenceDate, CancellationToken cancellationToken = default);
}
