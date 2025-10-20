using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.ValueObjects;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class AgendaEvent : Entity
{
    public AgendaEvent(
        Guid id,
        AgendaEventType type,
        Guid propertyId,
        string title,
        TimeRange timeRange,
        string responsible,
        IEnumerable<string> participants) : base(id)
    {
        Type = type;
        PropertyId = propertyId;
        Title = title;
        TimeRange = timeRange;
        Responsible = responsible;
        Participants = participants.ToList();
    }

    public AgendaEventType Type { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Title { get; private set; }
    public TimeRange TimeRange { get; private set; }
    public string Responsible { get; private set; }
    public List<string> Participants { get; }

    public void Reschedule(TimeRange newRange)
    {
        TimeRange = newRange;
    }

    public void Reassign(string responsible)
    {
        Responsible = responsible;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
    }

    public void AddParticipant(string participant)
    {
        if (!Participants.Contains(participant, StringComparer.OrdinalIgnoreCase))
        {
            Participants.Add(participant);
        }
    }

    public void RemoveParticipant(string participant)
    {
        Participants.RemoveAll(p => string.Equals(p, participant, StringComparison.OrdinalIgnoreCase));
    }
}
