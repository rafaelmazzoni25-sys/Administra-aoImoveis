using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class CommunicationService
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly INotificationRepository _notificationRepository;

    public CommunicationService(ICommunicationRepository communicationRepository, INotificationRepository notificationRepository)
    {
        _communicationRepository = communicationRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<CommunicationThread> CreateThreadAsync(CommunicationThread thread, CancellationToken cancellationToken = default)
    {
        await _communicationRepository.AddAsync(thread, cancellationToken);
        return thread;
    }

    public async Task<CommunicationMessage> PostMessageAsync(Guid threadId, string author, string message, IEnumerable<string>? mentions, CancellationToken cancellationToken = default)
    {
        var thread = await EnsureThreadAsync(threadId, cancellationToken);
        var posted = thread.PostMessage(author, message, mentions);
        await _communicationRepository.UpdateAsync(thread, cancellationToken);

        foreach (var mention in posted.Mentions)
        {
            var notification = new NotificationMessage(Guid.NewGuid(), mention, $"Nova menção em {thread.Title}", message, Domain.Enums.NotificationSeverity.Info, thread.ContextType.ToString());
            await _notificationRepository.AddAsync(notification, cancellationToken);
        }

        return posted;
    }

    public async Task ArchiveThreadAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        var thread = await EnsureThreadAsync(threadId, cancellationToken);
        thread.Archive();
        await _communicationRepository.UpdateAsync(thread, cancellationToken);
    }

    private async Task<CommunicationThread> EnsureThreadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _communicationRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Conversa não localizada");
    }
}
