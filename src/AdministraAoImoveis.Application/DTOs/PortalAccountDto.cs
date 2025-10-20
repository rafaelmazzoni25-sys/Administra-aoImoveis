using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record PortalAccountDto(
    Guid Id,
    string Email,
    string Name,
    PortalRole Role,
    bool Active,
    DateTime CreatedAt,
    IReadOnlyCollection<PortalAccessLogDto> Logs);

public sealed record PortalAccessLogDto(DateTime OccurredAt, string Description);
