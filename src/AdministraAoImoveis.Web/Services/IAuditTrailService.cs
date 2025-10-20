using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Services;

public interface IAuditTrailService
{
    Task RegisterAsync(string entityName, Guid entityId, string operation, string before, string after, string user, string ip, string host, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditLogEntry>> GetByPeriodAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
}
