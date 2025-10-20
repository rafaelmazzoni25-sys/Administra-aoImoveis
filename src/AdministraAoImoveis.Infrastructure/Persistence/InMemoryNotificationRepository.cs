using System.Linq;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryNotificationRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        _store.Notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<NotificationMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.Notifications.TryGetValue(id, out var notification);
        return Task.FromResult<NotificationMessage?>(notification);
    }

    public Task<IReadOnlyCollection<NotificationMessage>> GetPendingAsync(string recipient, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<NotificationMessage> result = _store.Notifications.Values
            .Where(n => string.Equals(n.Recipient, recipient, StringComparison.OrdinalIgnoreCase) && !n.ReadAt.HasValue)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        _store.Notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }
}
