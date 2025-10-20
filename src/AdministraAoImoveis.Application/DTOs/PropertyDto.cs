using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record PropertyDto(
    Guid Id,
    string Code,
    string Address,
    string Type,
    decimal SizeSquareMeters,
    int Bedrooms,
    string OwnerName,
    PropertyOperationalStatus Status,
    DateTime? AvailableFrom,
    Guid? ActiveNegotiationId,
    bool HasOpenMaintenance,
    bool HasOpenPending);
