using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class TaskAppService
{
    private readonly TaskManagementService _taskManagementService;
    private readonly ITaskRepository _taskRepository;

    public TaskAppService(TaskManagementService taskManagementService, ITaskRepository taskRepository)
    {
        _taskManagementService = taskManagementService;
        _taskRepository = taskRepository;
    }

    public async Task<TaskDto> CreateAsync(TaskType type, string title, string description, string sector, string owner, TaskPriority priority, DateTime? dueDate, CancellationToken cancellationToken = default)
    {
        var task = new TaskItem(Guid.NewGuid(), type, title, description, sector, owner, priority, DateTime.UtcNow, dueDate, TaskStatus.Aberta);
        await _taskManagementService.CreateAsync(task, cancellationToken);
        return task.ToDto();
    }

    public async Task<TaskDto> UpdateStatusAsync(Guid taskId, TaskStatus status, string user, string notes, CancellationToken cancellationToken = default)
    {
        var task = await _taskManagementService.UpdateStatusAsync(taskId, status, user, notes, cancellationToken);
        return task.ToDto();
    }

    public async Task<IReadOnlyCollection<TaskDto>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetOverdueAsync(DateTime.UtcNow, cancellationToken);
        return tasks.Select(t => t.ToDto()).ToList();
    }
}
