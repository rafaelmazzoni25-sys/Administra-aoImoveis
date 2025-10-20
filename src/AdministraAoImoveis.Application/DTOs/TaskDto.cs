using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record TaskDto(
    Guid Id,
    TaskType Type,
    string Title,
    string Description,
    string Sector,
    string Owner,
    TaskPriority Priority,
    DateTime CreatedAt,
    DateTime? DueDate,
    TaskStatus Status,
    IReadOnlyCollection<string> History);
