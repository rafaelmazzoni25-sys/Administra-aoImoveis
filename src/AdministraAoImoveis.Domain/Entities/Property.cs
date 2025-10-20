using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class Property : Entity
{
    private readonly List<PropertyStatusHistory> _history = new();

    public Property(
        Guid id,
        string code,
        string address,
        string type,
        decimal sizeSquareMeters,
        int bedrooms,
        string ownerName,
        PropertyOperationalStatus status,
        DateTime? availableFrom,
        string? description = null) : base(id)
    {
        Code = code;
        Address = address;
        Type = type;
        SizeSquareMeters = sizeSquareMeters;
        Bedrooms = bedrooms;
        OwnerName = ownerName;
        Status = status;
        AvailableFrom = availableFrom;
        Description = description;
        RegisterHistory(status, "Cadastro inicial");
    }

    public string Code { get; private set; }
    public string Address { get; private set; }
    public string Type { get; private set; }
    public decimal SizeSquareMeters { get; private set; }
    public int Bedrooms { get; private set; }
    public string OwnerName { get; private set; }
    public string? Description { get; private set; }
    public PropertyOperationalStatus Status { get; private set; }
    public DateTime? AvailableFrom { get; private set; }
    public IReadOnlyCollection<PropertyStatusHistory> History => _history.AsReadOnly();
    public Guid? ActiveNegotiationId { get; private set; }
    public bool HasOpenMaintenance { get; private set; }
    public bool HasOpenPending { get; private set; }

    public void UpdateBasicInfo(string address, string type, decimal sizeSquareMeters, int bedrooms, string ownerName, string? description)
    {
        Address = address;
        Type = type;
        SizeSquareMeters = sizeSquareMeters;
        Bedrooms = bedrooms;
        OwnerName = ownerName;
        Description = description;
    }

    public void ScheduleAvailability(DateTime availableFrom)
    {
        AvailableFrom = availableFrom;
        ChangeStatus(PropertyOperationalStatus.AgendadoParaDisponibilizacao, "Disponibilizacao agendada");
    }

    public void MarkMaintenance(bool open)
    {
        HasOpenMaintenance = open;
        if (open)
        {
            ChangeStatus(PropertyOperationalStatus.EmManutencao, "Manutencao aberta");
        }
        else if (Status == PropertyOperationalStatus.EmManutencao)
        {
            ChangeStatus(PropertyOperationalStatus.Disponivel, "Manutencao concluida");
        }
    }

    public void MarkPending(bool open)
    {
        HasOpenPending = open;
        if (open && Status == PropertyOperationalStatus.Disponivel)
        {
            ChangeStatus(PropertyOperationalStatus.Indisponivel, "Pendencia aberta");
        }
        else if (!open && Status == PropertyOperationalStatus.Indisponivel)
        {
            ChangeStatus(PropertyOperationalStatus.Disponivel, "Pendencia resolvida");
        }
    }

    public void AttachNegotiation(Guid negotiationId)
    {
        if (ActiveNegotiationId.HasValue && ActiveNegotiationId != negotiationId)
        {
            throw new InvalidOperationException("O imóvel já está em negociação ativa.");
        }

        ActiveNegotiationId = negotiationId;
        ChangeStatus(PropertyOperationalStatus.EmNegociacao, "Negociacao vinculada");
    }

    public void ReleaseNegotiation(Guid negotiationId)
    {
        if (ActiveNegotiationId == negotiationId)
        {
            ActiveNegotiationId = null;
            ChangeStatus(PropertyOperationalStatus.Disponivel, "Negociacao encerrada");
        }
    }

    public void ChangeStatus(PropertyOperationalStatus newStatus, string reason)
    {
        if (Status == newStatus)
        {
            return;
        }

        Status = newStatus;
        RegisterHistory(newStatus, reason);
    }

    private void RegisterHistory(PropertyOperationalStatus status, string reason)
    {
        _history.Add(new PropertyStatusHistory(status, DateTime.UtcNow, reason));
    }
}

public sealed record PropertyStatusHistory(PropertyOperationalStatus Status, DateTime OccurredAt, string Reason);
