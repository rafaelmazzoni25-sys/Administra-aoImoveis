using System.Linq;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class TaskManagementService
{
    private readonly ITaskRepository _taskRepository;

    public TaskManagementService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _taskRepository.AddAsync(task, cancellationToken);
        return task;
    }

    public async Task<TaskItem> UpdateStatusAsync(Guid taskId, TaskStatus status, string user, string notes, CancellationToken cancellationToken = default)
    {
        var task = await EnsureTaskAsync(taskId, cancellationToken);
        task.ChangeStatus(status, user, notes);
        await _taskRepository.UpdateAsync(task, cancellationToken);
        return task;
    }

    public async Task<IReadOnlyList<TaskItem>> EscalateOverdueAsync(DateTime referenceDate, CancellationToken cancellationToken = default)
    {
        var overdue = await _taskRepository.GetOverdueAsync(referenceDate, cancellationToken);
        foreach (var task in overdue)
        {
            task.ChangePriority(TaskPriority.Critica, "sistema");
        }

        foreach (var task in overdue)
        {
            await _taskRepository.UpdateAsync(task, cancellationToken);
        }

        return overdue;
    }

    public async Task<IDictionary<PriorityQuadrant, IReadOnlyCollection<TaskItem>>> BuildPriorityMatrixAsync(CancellationToken cancellationToken = default)
    {
        var all = await _taskRepository.GetAllAsync(cancellationToken);
        var result = new Dictionary<PriorityQuadrant, IReadOnlyCollection<TaskItem>>
        {
            [PriorityQuadrant.Critical] = Array.Empty<TaskItem>(),
            [PriorityQuadrant.High] = Array.Empty<TaskItem>(),
            [PriorityQuadrant.Standard] = Array.Empty<TaskItem>(),
            [PriorityQuadrant.Opportunity] = Array.Empty<TaskItem>()
        };

        result[PriorityQuadrant.Critical] = all.Where(t => t.Priority == TaskPriority.Critica || t.IsOverdue(DateTime.UtcNow)).ToList();
        result[PriorityQuadrant.High] = all.Where(t => t.Priority == TaskPriority.Alta && !t.IsOverdue(DateTime.UtcNow)).ToList();
        result[PriorityQuadrant.Standard] = all.Where(t => t.Priority == TaskPriority.Media && !t.IsOverdue(DateTime.UtcNow)).ToList();
        result[PriorityQuadrant.Opportunity] = all.Where(t => t.Priority == TaskPriority.Baixa && !t.IsOverdue(DateTime.UtcNow)).ToList();

        return result;
    }

    private async Task<TaskItem> EnsureTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _taskRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Tarefa não encontrada");
    }
}
