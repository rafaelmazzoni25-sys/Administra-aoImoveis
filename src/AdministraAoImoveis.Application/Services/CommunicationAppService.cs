using System.Linq;
using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class CommunicationAppService
{
    private readonly CommunicationService _communicationService;
    private readonly ICommunicationRepository _communicationRepository;

    public CommunicationAppService(CommunicationService communicationService, ICommunicationRepository communicationRepository)
    {
        _communicationService = communicationService;
        _communicationRepository = communicationRepository;
    }

    public async Task<CommunicationDto> CreateThreadAsync(CommunicationContextType contextType, Guid contextId, string title, IEnumerable<string> participants, CancellationToken cancellationToken = default)
    {
        var thread = new CommunicationThread(Guid.NewGuid(), contextType, contextId, title, participants);
        await _communicationService.CreateThreadAsync(thread, cancellationToken);
        return thread.ToDto();
    }

    public async Task<CommunicationMessageDto> PostMessageAsync(Guid threadId, string author, string message, IEnumerable<string>? mentions, CancellationToken cancellationToken = default)
    {
        var posted = await _communicationService.PostMessageAsync(threadId, author, message, mentions, cancellationToken);
        return new CommunicationMessageDto(posted.Id, posted.Author, posted.Content, posted.SentAt, posted.Mentions);
    }

    public async Task ArchiveThreadAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        await _communicationService.ArchiveThreadAsync(threadId, cancellationToken);
    }

    public async Task<CommunicationDto> GetAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        var thread = await _communicationRepository.GetByIdAsync(threadId, cancellationToken) ?? throw new InvalidOperationException("Conversa não encontrada");
        return thread.ToDto();
    }
}
