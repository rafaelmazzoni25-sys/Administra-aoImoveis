using System.Linq;
using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class NotificationAppService
{
    private readonly NotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;

    public NotificationAppService(NotificationService notificationService, INotificationRepository notificationRepository)
    {
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
    }

    public async Task<NotificationDto> NotifyAsync(string recipient, string title, string message, NotificationSeverity severity, string module, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationService.NotifyAsync(recipient, title, message, severity, module, cancellationToken);
        return notification.ToDto();
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        await _notificationService.MarkReadAsync(notificationId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<NotificationDto>> GetPendingAsync(string recipient, CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.GetPendingAsync(recipient, cancellationToken);
        return notifications.Select(n => n.ToDto()).ToList();
    }
}
