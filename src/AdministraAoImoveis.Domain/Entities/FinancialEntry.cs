using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.ValueObjects;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class FinancialEntry : Entity
{
    private readonly List<FinancialHistory> _history = new();

    public FinancialEntry(
        Guid id,
        Guid referenceId,
        string referenceType,
        FinancialEntryType type,
        Money amount,
        DateTime dueDate,
        bool blocksAvailability) : base(id)
    {
        ReferenceId = referenceId;
        ReferenceType = referenceType;
        Type = type;
        Amount = amount;
        DueDate = dueDate;
        BlocksAvailability = blocksAvailability;
        Status = FinancialEntryStatus.Pending;
        _history.Add(new FinancialHistory(DateTime.UtcNow, Status, "Lançamento criado"));
    }

    public Guid ReferenceId { get; }
    public string ReferenceType { get; }
    public FinancialEntryType Type { get; }
    public Money Amount { get; private set; }
    public DateTime DueDate { get; private set; }
    public bool BlocksAvailability { get; }
    public FinancialEntryStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public IReadOnlyCollection<FinancialHistory> History => _history.AsReadOnly();

    public void UpdateAmount(Money amount)
    {
        Amount = amount;
        _history.Add(new FinancialHistory(DateTime.UtcNow, Status, "Valor atualizado"));
    }

    public void Reschedule(DateTime dueDate)
    {
        DueDate = dueDate;
        _history.Add(new FinancialHistory(DateTime.UtcNow, Status, "Vencimento reprogramado"));
    }

    public void MarkReceived(DateTime receivedAt)
    {
        Status = FinancialEntryStatus.Received;
        PaidAt = receivedAt;
        _history.Add(new FinancialHistory(DateTime.UtcNow, Status, "Pagamento recebido"));
    }

    public void MarkOverdue()
    {
        if (Status == FinancialEntryStatus.Pending && DueDate < DateTime.UtcNow.Date)
        {
            Status = FinancialEntryStatus.Overdue;
            _history.Add(new FinancialHistory(DateTime.UtcNow, Status, "Pagamento em atraso"));
        }
    }

    public void Cancel(string reason)
    {
        Status = FinancialEntryStatus.Cancelled;
        _history.Add(new FinancialHistory(DateTime.UtcNow, Status, reason));
    }
}

public sealed record FinancialHistory(DateTime OccurredAt, FinancialEntryStatus Status, string Notes);
