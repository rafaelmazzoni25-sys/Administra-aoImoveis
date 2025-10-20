using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditLogEntry>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
