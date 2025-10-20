namespace AdministraAoImoveis.Application.DTOs;

public sealed record AuditLogDto(Guid Id, string Actor, string Action, string Target, string Details, DateTime OccurredAt);
