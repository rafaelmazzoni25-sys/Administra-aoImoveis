using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record InspectionDto(
    Guid Id,
    Guid PropertyId,
    InspectionType Type,
    DateTime ScheduledFor,
    string Responsible,
    InspectionStatus Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    IReadOnlyCollection<string> ChecklistItems,
    IReadOnlyCollection<string> Photos);
