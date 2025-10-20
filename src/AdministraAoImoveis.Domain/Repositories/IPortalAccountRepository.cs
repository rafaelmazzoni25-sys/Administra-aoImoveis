using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface IPortalAccountRepository
{
    Task AddAsync(PortalAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(PortalAccount account, CancellationToken cancellationToken = default);
    Task<PortalAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PortalAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
