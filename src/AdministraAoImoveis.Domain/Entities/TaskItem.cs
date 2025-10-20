using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class TaskItem : Entity
{
    private readonly List<TaskUpdate> _updates = new();

    public TaskItem(
        Guid id,
        TaskType type,
        string title,
        string description,
        string sector,
        string owner,
        TaskPriority priority,
        DateTime createdAt,
        DateTime? dueDate,
        TaskStatus status) : base(id)
    {
        Type = type;
        Title = title;
        Description = description;
        Sector = sector;
        Owner = owner;
        Priority = priority;
        CreatedAt = createdAt;
        DueDate = dueDate;
        Status = status;
        _updates.Add(new TaskUpdate(DateTime.UtcNow, "Tarefa criada", owner));
    }

    public TaskType Type { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Sector { get; private set; }
    public string Owner { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? DueDate { get; private set; }
    public TaskStatus Status { get; private set; }
    public IReadOnlyCollection<TaskUpdate> Updates => _updates.AsReadOnly();

    public void AssignTo(string owner)
    {
        Owner = owner;
        _updates.Add(new TaskUpdate(DateTime.UtcNow, $"Responsável alterado para {owner}", owner));
    }

    public void ChangeStatus(TaskStatus status, string user, string notes)
    {
        Status = status;
        _updates.Add(new TaskUpdate(DateTime.UtcNow, notes, user));
    }

    public void ChangePriority(TaskPriority priority, string user)
    {
        Priority = priority;
        _updates.Add(new TaskUpdate(DateTime.UtcNow, $"Prioridade alterada para {priority}", user));
    }

    public void Reschedule(DateTime? dueDate, string user)
    {
        DueDate = dueDate;
        _updates.Add(new TaskUpdate(DateTime.UtcNow, dueDate.HasValue ? $"Prazo ajustado para {dueDate:dd/MM/yyyy}" : "Prazo removido", user));
    }

    public bool IsOverdue(DateTime now) => DueDate.HasValue && now > DueDate.Value && Status is not (TaskStatus.Concluida or TaskStatus.Cancelada);
}

public sealed record TaskUpdate(DateTime OccurredAt, string Message, string User);
