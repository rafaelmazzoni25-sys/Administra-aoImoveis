using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    string Recipient,
    string Title,
    string Message,
    NotificationSeverity Severity,
    string RelatedModule,
    DateTime CreatedAt,
    DateTime? ReadAt);
