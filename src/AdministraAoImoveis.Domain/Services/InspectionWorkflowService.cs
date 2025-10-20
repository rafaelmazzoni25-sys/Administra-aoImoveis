using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class InspectionWorkflowService
{
    private readonly IInspectionRepository _inspectionRepository;
    private readonly ITaskRepository _taskRepository;

    public InspectionWorkflowService(IInspectionRepository inspectionRepository, ITaskRepository taskRepository)
    {
        _inspectionRepository = inspectionRepository;
        _taskRepository = taskRepository;
    }

    public async Task<Inspection> ScheduleAsync(Inspection inspection, CancellationToken cancellationToken = default)
    {
        await _inspectionRepository.AddAsync(inspection, cancellationToken);
        return inspection;
    }

    public async Task<Inspection> CompleteAsync(Guid inspectionId, bool hasPending, IEnumerable<string> pendingDescriptions, CancellationToken cancellationToken = default)
    {
        var inspection = await EnsureInspectionAsync(inspectionId, cancellationToken);
        inspection.Finish(hasPending);
        if (hasPending)
        {
            foreach (var description in pendingDescriptions)
            {
                var task = new TaskItem(Guid.NewGuid(), TaskType.Pendencia, description, description, "Manutenção", inspection.Responsible, TaskPriority.Alta, DateTime.UtcNow, DateTime.UtcNow.AddDays(3), TaskStatus.Aberta);
                await _taskRepository.AddAsync(task, cancellationToken);
                inspection.LinkTask(task.Id, description);
            }
        }

        await _inspectionRepository.UpdateAsync(inspection, cancellationToken);
        return inspection;
    }

    public async Task<Inspection> ApproveReportAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        var inspection = await EnsureInspectionAsync(inspectionId, cancellationToken);
        inspection.ApproveReport();
        await _inspectionRepository.UpdateAsync(inspection, cancellationToken);
        return inspection;
    }

    private async Task<Inspection> EnsureInspectionAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _inspectionRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Vistoria não encontrada");
    }
}
