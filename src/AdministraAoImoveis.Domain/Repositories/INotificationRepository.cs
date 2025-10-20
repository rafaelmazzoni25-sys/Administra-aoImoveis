using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface INotificationRepository
{
    Task AddAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
    Task<NotificationMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificationMessage>> GetPendingAsync(string recipient, CancellationToken cancellationToken = default);
}
