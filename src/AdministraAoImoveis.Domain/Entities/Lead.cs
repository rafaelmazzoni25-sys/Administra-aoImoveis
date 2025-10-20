using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class Lead : Entity
{
    private readonly List<LeadInteraction> _interactions = new();

    public Lead(
        Guid id,
        string name,
        string contact,
        LeadSource source,
        string desiredPropertyType,
        decimal? budget) : base(id)
    {
        Name = name;
        Contact = contact;
        Source = source;
        DesiredPropertyType = desiredPropertyType;
        Budget = budget;
        Status = LeadStatus.Captured;
        CreatedAt = DateTime.UtcNow;
        _interactions.Add(new LeadInteraction(DateTime.UtcNow, "Lead captado", "Sistema"));
    }

    public string Name { get; private set; }
    public string Contact { get; private set; }
    public LeadSource Source { get; private set; }
    public string DesiredPropertyType { get; private set; }
    public decimal? Budget { get; private set; }
    public LeadStatus Status { get; private set; }
    public string? AssignedTo { get; private set; }
    public DateTime CreatedAt { get; }
    public IReadOnlyCollection<LeadInteraction> Interactions => _interactions.AsReadOnly();

    public void UpdateProfile(string name, string contact, string desiredPropertyType, decimal? budget)
    {
        Name = name;
        Contact = contact;
        DesiredPropertyType = desiredPropertyType;
        Budget = budget;
        _interactions.Add(new LeadInteraction(DateTime.UtcNow, "Perfil atualizado", "Sistema"));
    }

    public void AssignTo(string user)
    {
        AssignedTo = user;
        _interactions.Add(new LeadInteraction(DateTime.UtcNow, $"Responsável definido: {user}", "Sistema"));
    }

    public void RegisterInteraction(string description, string actor)
    {
        _interactions.Add(new LeadInteraction(DateTime.UtcNow, description, actor));
    }

    public void ChangeStatus(LeadStatus status, string actor)
    {
        Status = status;
        _interactions.Add(new LeadInteraction(DateTime.UtcNow, $"Status alterado para {status}", actor));
    }
}

public sealed record LeadInteraction(DateTime OccurredAt, string Description, string Actor);
