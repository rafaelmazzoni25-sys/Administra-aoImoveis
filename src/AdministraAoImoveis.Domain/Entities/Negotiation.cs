using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class Negotiation : Entity
{
    private readonly List<NegotiationHistory> _history = new();

    public Negotiation(
        Guid id,
        Guid propertyId,
        string interestedName,
        string interestedEmail,
        string brokerName,
        NegotiationStage stage,
        DateTime createdAt) : base(id)
    {
        PropertyId = propertyId;
        InterestedName = interestedName;
        InterestedEmail = interestedEmail;
        BrokerName = brokerName;
        Stage = stage;
        CreatedAt = createdAt;
        RegisterStage(stage, "Negociação criada");
    }

    public Guid PropertyId { get; }
    public string InterestedName { get; private set; }
    public string InterestedEmail { get; private set; }
    public string BrokerName { get; private set; }
    public NegotiationStage Stage { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime? ProposalExpiresAt { get; private set; }
    public decimal SignalAmount { get; private set; }
    public bool PropertyBlocked => Stage is not (NegotiationStage.Cancelada or NegotiationStage.Concluida);
    public IReadOnlyCollection<NegotiationHistory> History => _history.AsReadOnly();

    public void UpdateInterested(string name, string email)
    {
        InterestedName = name;
        InterestedEmail = email;
    }

    public void UpdateBroker(string name)
    {
        BrokerName = name;
    }

    public void RegisterSignal(decimal amount)
    {
        SignalAmount = amount;
    }

    public void SetProposalExpiration(DateTime? expiration)
    {
        ProposalExpiresAt = expiration;
    }

    public void AdvanceTo(NegotiationStage stage, string reason)
    {
        if (Stage == stage)
        {
            return;
        }

        if (Stage == NegotiationStage.Cancelada)
        {
            throw new InvalidOperationException("Negociação cancelada não pode avançar.");
        }

        Stage = stage;
        if (stage is NegotiationStage.Concluida or NegotiationStage.Cancelada)
        {
            ClosedAt = DateTime.UtcNow;
        }

        RegisterStage(stage, reason);
    }

    public bool IsExpired(DateTime now) => ProposalExpiresAt.HasValue && ProposalExpiresAt.Value < now && Stage == NegotiationStage.PropostaEnviada;

    private void RegisterStage(NegotiationStage stage, string reason)
    {
        _history.Add(new NegotiationHistory(stage, DateTime.UtcNow, reason));
    }
}

public sealed record NegotiationHistory(NegotiationStage Stage, DateTime OccurredAt, string Reason);
