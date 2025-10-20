using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class Inspection : Entity
{
    private readonly List<string> _checklistItems = new();
    private readonly List<string> _photos = new();

    public Inspection(
        Guid id,
        Guid propertyId,
        InspectionType type,
        DateTime scheduledFor,
        string responsible,
        InspectionStatus status) : base(id)
    {
        PropertyId = propertyId;
        Type = type;
        ScheduledFor = scheduledFor;
        Responsible = responsible;
        Status = status;
    }

    public Guid PropertyId { get; }
    public InspectionType Type { get; private set; }
    public DateTime ScheduledFor { get; private set; }
    public string Responsible { get; private set; }
    public InspectionStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public IReadOnlyCollection<string> ChecklistItems => _checklistItems.AsReadOnly();
    public IReadOnlyCollection<string> Photos => _photos.AsReadOnly();
    public IReadOnlyCollection<TaskLink> GeneratedTasks => _generatedTasks.AsReadOnly();

    private readonly List<TaskLink> _generatedTasks = new();

    public void Reschedule(DateTime when)
    {
        ScheduledFor = when;
    }

    public void Assign(string responsible)
    {
        Responsible = responsible;
    }

    public void Start()
    {
        Status = InspectionStatus.EmAndamento;
        StartedAt = DateTime.UtcNow;
    }

    public void Finish(bool hasPending)
    {
        Status = hasPending ? InspectionStatus.RelatorioPendente : InspectionStatus.Concluida;
        FinishedAt = DateTime.UtcNow;
    }

    public void ApproveReport()
    {
        Status = InspectionStatus.EmAprovacao;
    }

    public void Complete()
    {
        Status = InspectionStatus.Concluida;
    }

    public void Cancel(string reason)
    {
        Status = InspectionStatus.Cancelada;
        _generatedTasks.Add(new TaskLink(Guid.Empty, reason));
    }

    public void AddChecklistItem(string item)
    {
        if (!_checklistItems.Contains(item, StringComparer.OrdinalIgnoreCase))
        {
            _checklistItems.Add(item);
        }
    }

    public void AttachPhoto(string url)
    {
        _photos.Add(url);
    }

    public void LinkTask(Guid taskId, string description)
    {
        _generatedTasks.Add(new TaskLink(taskId, description));
    }
}

public sealed record TaskLink(Guid TaskId, string Description);
