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

    private async Task<TaskItem> EnsureTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _taskRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Tarefa não encontrada");
    }
}
