using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdministraAoImoveis.Web.Services.DocumentExpiration;

public class PropertyDocumentExpirationService : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(6);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PropertyDocumentExpirationService> _logger;
    private readonly PropertyDocumentExpirationOptions _options;

    public PropertyDocumentExpirationService(
        IServiceProvider serviceProvider,
        IOptions<PropertyDocumentExpirationOptions> options,
        ILogger<PropertyDocumentExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = _options.Interval <= TimeSpan.Zero
            ? DefaultInterval
            : _options.Interval;

        _logger.LogInformation(
            "Serviço de expiração de documentos iniciado. Intervalo configurado: {Interval}.",
            interval);

        if (_options.RunOnStartup)
        {
            await RunExpirationAsync(stoppingToken);
        }

        try
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunExpirationAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Encerrando o serviço.
        }
    }

    private async Task RunExpirationAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var agora = DateTime.UtcNow;

            var expirados = await context.PropertyDocuments
                .Where(d => d.Status == DocumentStatus.Aprovado
                            && d.ValidoAte.HasValue
                            && d.ValidoAte.Value < agora)
                .ToListAsync(cancellationToken);

            if (expirados.Count == 0)
            {
                _logger.LogDebug("Nenhum documento expirado encontrado para atualização.");
                return;
            }

            foreach (var documento in expirados)
            {
                documento.Status = DocumentStatus.Expirado;
                documento.UpdatedAt = agora;
                documento.UpdatedBy = "Sistema";
            }

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "{Quantidade} documentos marcados como expirados automaticamente.",
                expirados.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao atualizar status de documentos expirados.");
        }
    }
}
