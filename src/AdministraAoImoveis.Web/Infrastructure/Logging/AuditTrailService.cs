using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdministraAoImoveis.Web.Infrastructure.Logging;

public class AuditTrailService : IAuditTrailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditTrailService> _logger;
    private readonly string _logDirectory;

    public AuditTrailService(ApplicationDbContext context, ILogger<AuditTrailService> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _logDirectory = Path.Combine(environment.ContentRootPath, "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public async Task RegisterAsync(string entityName, Guid entityId, string operation, string before, string after, string user, string ip, string host, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Entidade = entityName,
            EntidadeId = entityId,
            Operacao = operation,
            Antes = before,
            Depois = after,
            Usuario = user,
            Ip = ip,
            Host = host,
            RegistradoEm = DateTime.UtcNow
        };

        _context.AuditTrail.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        var line = $"{entry.RegistradoEm:O};{entityName};{entityId};{operation};{user};{ip};{host}";
        var filePath = Path.Combine(_logDirectory, $"audit-{DateTime.UtcNow:yyyyMMdd}.log");
        await File.AppendAllLinesAsync(filePath, new[] { line }, cancellationToken);

        _logger.LogInformation("Auditoria registrada para {Entity} {Id}", entityName, entityId);
    }

    public async Task<IReadOnlyCollection<AuditLogEntry>> GetByPeriodAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        return await _context.AuditTrail
            .Where(a => a.RegistradoEm >= start && a.RegistradoEm <= end)
            .OrderByDescending(a => a.RegistradoEm)
            .ToListAsync(cancellationToken);
    }
}
