using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.ValueObjects;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record FinancialEntryDto(
    Guid Id,
    Guid ReferenceId,
    string ReferenceType,
    FinancialEntryType Type,
    Money Amount,
    DateTime DueDate,
    bool BlocksAvailability,
    FinancialEntryStatus Status,
    DateTime? PaidAt,
    IReadOnlyCollection<FinancialHistoryDto> History);

public sealed record FinancialHistoryDto(DateTime OccurredAt, FinancialEntryStatus Status, string Notes);
