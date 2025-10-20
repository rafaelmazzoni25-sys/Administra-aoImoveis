using System.Linq;
using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class CommunicationThread : Entity
{
    private readonly List<string> _participants = new();
    private readonly List<CommunicationMessage> _messages = new();

    public CommunicationThread(
        Guid id,
        CommunicationContextType contextType,
        Guid contextId,
        string title,
        IEnumerable<string> participants) : base(id)
    {
        ContextType = contextType;
        ContextId = contextId;
        Title = title;
        _participants.AddRange(participants);
        CreatedAt = DateTime.UtcNow;
    }

    public CommunicationContextType ContextType { get; }
    public Guid ContextId { get; }
    public string Title { get; private set; }
    public DateTime CreatedAt { get; }
    public bool Archived { get; private set; }
    public IReadOnlyCollection<string> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<CommunicationMessage> Messages => _messages.AsReadOnly();

    public void Rename(string title)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }
    }

    public void AddParticipant(string participant)
    {
        if (!_participants.Contains(participant))
        {
            _participants.Add(participant);
        }
    }

    public void RemoveParticipant(string participant)
    {
        _participants.Remove(participant);
    }

    public CommunicationMessage PostMessage(string author, string content, IEnumerable<string>? mentions)
    {
        if (Archived)
        {
            throw new InvalidOperationException("Thread arquivada não aceita novas mensagens.");
        }

        var message = new CommunicationMessage(
            Guid.NewGuid(),
            author,
            content,
            DateTime.UtcNow,
            mentions?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>());
        _messages.Add(message);
        return message;
    }

    public void Archive()
    {
        Archived = true;
    }
}

public sealed record CommunicationMessage(Guid Id, string Author, string Content, DateTime SentAt, IReadOnlyCollection<string> Mentions);
