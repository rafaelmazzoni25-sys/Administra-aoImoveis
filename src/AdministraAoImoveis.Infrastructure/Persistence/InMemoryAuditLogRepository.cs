using System.Linq;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryAuditLogRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        _store.AuditLogs[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AuditLogEntry>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<AuditLogEntry> result = _store.AuditLogs.Values
            .OrderByDescending(log => log.OccurredAt)
            .Take(take)
            .ToList();
        return Task.FromResult(result);
    }
}
