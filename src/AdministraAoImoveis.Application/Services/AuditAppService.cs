using System.Linq;
using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class AuditAppService
{
    private readonly AuditTrailService _auditTrailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditAppService(AuditTrailService auditTrailService, IAuditLogRepository auditLogRepository)
    {
        _auditTrailService = auditTrailService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<AuditLogDto> RecordAsync(string actor, string action, string target, string details, CancellationToken cancellationToken = default)
    {
        var entry = await _auditTrailService.RecordAsync(actor, action, target, details, cancellationToken);
        return entry.ToDto();
    }

    public async Task<IReadOnlyCollection<AuditLogDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        var logs = await _auditLogRepository.GetRecentAsync(take, cancellationToken);
        return logs.Select(l => l.ToDto()).ToList();
    }
}
