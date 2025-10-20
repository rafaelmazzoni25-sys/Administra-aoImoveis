using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record LeadDto(
    Guid Id,
    string Name,
    string Contact,
    LeadSource Source,
    string DesiredPropertyType,
    decimal? Budget,
    LeadStatus Status,
    string? AssignedTo,
    DateTime CreatedAt,
    IReadOnlyCollection<LeadInteractionDto> Interactions);

public sealed record LeadInteractionDto(DateTime OccurredAt, string Description, string Actor);
