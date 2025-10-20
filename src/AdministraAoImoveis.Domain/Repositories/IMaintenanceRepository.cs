using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface IMaintenanceRepository
{
    Task<MaintenanceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MaintenanceOrder order, CancellationToken cancellationToken = default);
    Task UpdateAsync(MaintenanceOrder order, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceOrder>> GetOpenByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
}
