using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface IFinancialRepository
{
    Task AddAsync(FinancialEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(FinancialEntry entry, CancellationToken cancellationToken = default);
    Task<FinancialEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FinancialEntry>> GetBlockingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FinancialEntry>> GetBlockingByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
}
