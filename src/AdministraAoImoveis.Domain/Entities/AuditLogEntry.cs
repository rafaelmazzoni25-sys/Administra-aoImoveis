namespace AdministraAoImoveis.Domain.Entities;

public sealed class AuditLogEntry : Entity
{
    public AuditLogEntry(
        Guid id,
        string actor,
        string action,
        string target,
        string details) : base(id)
    {
        Actor = actor;
        Action = action;
        Target = target;
        Details = details;
        OccurredAt = DateTime.UtcNow;
    }

    public string Actor { get; }
    public string Action { get; }
    public string Target { get; }
    public string Details { get; }
    public DateTime OccurredAt { get; }
}
