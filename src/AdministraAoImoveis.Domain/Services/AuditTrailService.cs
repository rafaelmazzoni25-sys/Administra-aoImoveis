using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class AuditTrailService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditTrailService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<AuditLogEntry> RecordAsync(string actor, string action, string target, string details, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry(Guid.NewGuid(), actor, action, target, details);
        await _auditLogRepository.AddAsync(entry, cancellationToken);
        return entry;
    }
}
