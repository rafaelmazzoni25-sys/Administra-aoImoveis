using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class InspectionAppService
{
    private readonly InspectionWorkflowService _workflowService;
    private readonly IInspectionRepository _inspectionRepository;

    public InspectionAppService(InspectionWorkflowService workflowService, IInspectionRepository inspectionRepository)
    {
        _workflowService = workflowService;
        _inspectionRepository = inspectionRepository;
    }

    public async Task<InspectionDto> ScheduleAsync(Guid propertyId, InspectionType type, DateTime when, string responsible, CancellationToken cancellationToken = default)
    {
        var inspection = new Inspection(Guid.NewGuid(), propertyId, type, when, responsible, InspectionStatus.Agendada);
        await _workflowService.ScheduleAsync(inspection, cancellationToken);
        return inspection.ToDto();
    }

    public async Task<InspectionDto> CompleteAsync(Guid inspectionId, bool hasPending, IEnumerable<string> pendencias, CancellationToken cancellationToken = default)
    {
        var inspection = await _workflowService.CompleteAsync(inspectionId, hasPending, pendencias, cancellationToken);
        return inspection.ToDto();
    }

    public async Task<IReadOnlyCollection<InspectionDto>> GetScheduledForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var inspections = await _inspectionRepository.GetScheduledForPropertyAsync(propertyId, cancellationToken);
        return inspections.Select(i => i.ToDto()).ToList();
    }
}
