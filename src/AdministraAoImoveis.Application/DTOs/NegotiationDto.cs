using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record NegotiationDto(
    Guid Id,
    Guid PropertyId,
    string InterestedName,
    string InterestedEmail,
    string BrokerName,
    NegotiationStage Stage,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    DateTime? ProposalExpiresAt,
    decimal SignalAmount);
