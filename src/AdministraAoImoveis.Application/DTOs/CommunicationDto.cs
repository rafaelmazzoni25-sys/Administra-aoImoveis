using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record CommunicationDto(
    Guid Id,
    CommunicationContextType ContextType,
    Guid ContextId,
    string Title,
    DateTime CreatedAt,
    bool Archived,
    IReadOnlyCollection<string> Participants,
    IReadOnlyCollection<CommunicationMessageDto> Messages);

public sealed record CommunicationMessageDto(Guid Id, string Author, string Content, DateTime SentAt, IReadOnlyCollection<string> Mentions);
