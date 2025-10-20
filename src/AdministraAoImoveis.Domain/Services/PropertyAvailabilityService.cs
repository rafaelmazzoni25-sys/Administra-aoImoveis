using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class PropertyAvailabilityService
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly INegotiationRepository _negotiationRepository;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly ITaskRepository _taskRepository;

    public PropertyAvailabilityService(
        IPropertyRepository propertyRepository,
        INegotiationRepository negotiationRepository,
        IMaintenanceRepository maintenanceRepository,
        ITaskRepository taskRepository)
    {
        _propertyRepository = propertyRepository;
        _negotiationRepository = negotiationRepository;
        _maintenanceRepository = maintenanceRepository;
        _taskRepository = taskRepository;
    }

    public async Task<Property> RegisterPropertyAsync(Property property, CancellationToken cancellationToken = default)
    {
        await _propertyRepository.AddAsync(property, cancellationToken);
        return property;
    }

    public async Task<Property> HandleMoveOutNoticeAsync(Guid propertyId, DateTime moveOutDate, CancellationToken cancellationToken = default)
    {
        var property = await EnsurePropertyAsync(propertyId, cancellationToken);
        property.ChangeStatus(PropertyOperationalStatus.EmVistoriaSaida, "Aviso de desocupação recebido");
        property.ScheduleAvailability(moveOutDate.AddDays(1));
        await _propertyRepository.UpdateAsync(property, cancellationToken);
        return property;
    }

    public async Task<Property> OpenMaintenanceAsync(Guid propertyId, MaintenanceOrder order, CancellationToken cancellationToken = default)
    {
        var property = await EnsurePropertyAsync(propertyId, cancellationToken);
        property.MarkMaintenance(true);
        await _maintenanceRepository.AddAsync(order, cancellationToken);
        await _propertyRepository.UpdateAsync(property, cancellationToken);
        return property;
    }

    public async Task<Property> CloseMaintenanceAsync(Guid propertyId, Guid maintenanceId, CancellationToken cancellationToken = default)
    {
        var property = await EnsurePropertyAsync(propertyId, cancellationToken);
        var order = await _maintenanceRepository.GetByIdAsync(maintenanceId, cancellationToken) ?? throw new InvalidOperationException("OS não localizada");
        order.ChangeStatus(MaintenanceStatus.Concluida, "Concluída e liberada");
        await _maintenanceRepository.UpdateAsync(order, cancellationToken);

        var open = await _maintenanceRepository.GetOpenByPropertyAsync(propertyId, cancellationToken);
        property.MarkMaintenance(open.Any());
        await _propertyRepository.UpdateAsync(property, cancellationToken);
        return property;
    }

    public async Task<Property> SyncPendingStatusAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var property = await EnsurePropertyAsync(propertyId, cancellationToken);
        var pendencias = await _taskRepository.GetOverdueAsync(DateTime.UtcNow, cancellationToken);
        property.MarkPending(pendencias.Any(p => p.Type == TaskType.Pendencia && p.Status is not TaskStatus.Concluida and not TaskStatus.Cancelada));
        await _propertyRepository.UpdateAsync(property, cancellationToken);
        return property;
    }

    public async Task<Property> AttachNegotiationAsync(Guid propertyId, Guid negotiationId, CancellationToken cancellationToken = default)
    {
        var property = await EnsurePropertyAsync(propertyId, cancellationToken);
        var active = await _negotiationRepository.GetActiveByPropertyAsync(propertyId, cancellationToken);
        if (active.Any(n => n.Id != negotiationId && n.PropertyBlocked))
        {
            throw new InvalidOperationException("O imóvel já possui negociação ativa.");
        }

        property.AttachNegotiation(negotiationId);
        await _propertyRepository.UpdateAsync(property, cancellationToken);
        return property;
    }

    public async Task<Property> ReleaseNegotiationAsync(Guid propertyId, Guid negotiationId, CancellationToken cancellationToken = default)
    {
        var property = await EnsurePropertyAsync(propertyId, cancellationToken);
        property.ReleaseNegotiation(negotiationId);
        await _propertyRepository.UpdateAsync(property, cancellationToken);
        return property;
    }

    private async Task<Property> EnsurePropertyAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _propertyRepository.GetByIdAsync(propertyId, cancellationToken) ?? throw new InvalidOperationException("Imóvel não encontrado");
    }
}
