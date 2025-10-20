using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class NotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<NotificationMessage> NotifyAsync(string recipient, string title, string message, Domain.Enums.NotificationSeverity severity, string module, CancellationToken cancellationToken = default)
    {
        var notification = new NotificationMessage(Guid.NewGuid(), recipient, title, message, severity, module);
        await _notificationRepository.AddAsync(notification, cancellationToken);
        return notification;
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken) ?? throw new InvalidOperationException("Notificação não encontrada");
        notification.MarkRead();
        await _notificationRepository.UpdateAsync(notification, cancellationToken);
    }
}
