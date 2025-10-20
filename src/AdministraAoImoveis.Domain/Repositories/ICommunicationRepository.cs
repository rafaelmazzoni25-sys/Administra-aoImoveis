using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface ICommunicationRepository
{
    Task AddAsync(CommunicationThread thread, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommunicationThread thread, CancellationToken cancellationToken = default);
    Task<CommunicationThread?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
