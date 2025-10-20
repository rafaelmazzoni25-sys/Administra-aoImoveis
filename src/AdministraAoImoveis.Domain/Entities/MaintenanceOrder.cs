using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class MaintenanceOrder : Entity
{
    private readonly List<MaintenanceHistory> _history = new();

    public MaintenanceOrder(
        Guid id,
        Guid propertyId,
        string title,
        string description,
        string supplier,
        MaintenanceStatus status,
        decimal? estimatedCost,
        DateTime createdAt) : base(id)
    {
        PropertyId = propertyId;
        Title = title;
        Description = description;
        Supplier = supplier;
        Status = status;
        EstimatedCost = estimatedCost;
        CreatedAt = createdAt;
        _history.Add(new MaintenanceHistory(DateTime.UtcNow, status, "OS criada"));
    }

    public Guid PropertyId { get; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Supplier { get; private set; }
    public MaintenanceStatus Status { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? FinalCost { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? CompletedAt { get; private set; }
    public IReadOnlyCollection<MaintenanceHistory> History => _history.AsReadOnly();

    public void ChangeStatus(MaintenanceStatus status, string notes)
    {
        Status = status;
        if (status == MaintenanceStatus.Concluida)
        {
            CompletedAt = DateTime.UtcNow;
        }

        _history.Add(new MaintenanceHistory(DateTime.UtcNow, status, notes));
    }

    public void AssignSupplier(string supplier)
    {
        Supplier = supplier;
        _history.Add(new MaintenanceHistory(DateTime.UtcNow, Status, $"Fornecedor alterado para {supplier}"));
    }

    public void UpdateCosts(decimal? estimated, decimal? finalValue)
    {
        EstimatedCost = estimated;
        FinalCost = finalValue;
        _history.Add(new MaintenanceHistory(DateTime.UtcNow, Status, "Custos atualizados"));
    }
}

public sealed record MaintenanceHistory(DateTime OccurredAt, MaintenanceStatus Status, string Notes);
