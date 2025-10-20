using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class NotificationMessage : Entity
{
    public NotificationMessage(
        Guid id,
        string recipient,
        string title,
        string message,
        NotificationSeverity severity,
        string relatedModule) : base(id)
    {
        Recipient = recipient;
        Title = title;
        Message = message;
        Severity = severity;
        RelatedModule = relatedModule;
        CreatedAt = DateTime.UtcNow;
    }

    public string Recipient { get; }
    public string Title { get; }
    public string Message { get; }
    public NotificationSeverity Severity { get; }
    public string RelatedModule { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ReadAt { get; private set; }

    public void MarkRead()
    {
        ReadAt = DateTime.UtcNow;
    }
}
