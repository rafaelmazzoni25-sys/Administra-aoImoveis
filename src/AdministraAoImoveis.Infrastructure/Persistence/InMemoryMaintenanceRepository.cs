using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryMaintenanceRepository : IMaintenanceRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryMaintenanceRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(MaintenanceOrder order, CancellationToken cancellationToken = default)
    {
        _store.MaintenanceOrders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task<MaintenanceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.MaintenanceOrders.TryGetValue(id, out var order);
        return Task.FromResult<MaintenanceOrder?>(order);
    }

    public Task<IReadOnlyList<MaintenanceOrder>> GetOpenByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var result = _store.MaintenanceOrders.Values
            .Where(o => o.PropertyId == propertyId && o.Status != MaintenanceStatus.Concluida && o.Status != MaintenanceStatus.Cancelada)
            .ToList();
        return Task.FromResult<IReadOnlyList<MaintenanceOrder>>(result);
    }

    public Task UpdateAsync(MaintenanceOrder order, CancellationToken cancellationToken = default)
    {
        _store.MaintenanceOrders[order.Id] = order;
        return Task.CompletedTask;
    }
}
