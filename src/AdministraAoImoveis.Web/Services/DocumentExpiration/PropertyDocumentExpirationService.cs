using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdministraAoImoveis.Web.Services.DocumentExpiration;

public class PropertyDocumentExpirationService : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(6);
    private const string SystemUser = "Sistema";

    private static readonly CultureInfo PtBrCulture = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly JsonSerializerOptions AuditSerializerOptions = new()
    {
        WriteIndented = false
    };

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
            var auditTrail = scope.ServiceProvider.GetRequiredService<IAuditTrailService>();

            var agora = DateTime.UtcNow;

            var expirados = await context.PropertyDocuments
                .Include(d => d.Imovel)
                    .ThenInclude(p => p!.Proprietario)
                .Where(d => d.Status == DocumentStatus.Aprovado
                            && d.ValidoAte.HasValue
                            && d.ValidoAte.Value < agora)
                .ToListAsync(cancellationToken);

            if (expirados.Count == 0)
            {
                _logger.LogDebug("Nenhum documento expirado encontrado para atualização.");
                return;
            }

            var notificacoes = new List<InAppNotification>();
            var auditorias = new List<PendingAudit>();

            foreach (var documento in expirados)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var antes = CreateAuditSnapshot(documento);

                documento.Status = DocumentStatus.Expirado;
                documento.UpdatedAt = agora;
                documento.UpdatedBy = SystemUser;

                var depois = CreateAuditSnapshot(documento);
                auditorias.Add(new PendingAudit(documento.Id, antes, depois));

                if (documento.Imovel?.Proprietario?.UsuarioId is { Length: > 0 } usuarioId)
                {
                    var tituloImovel = string.IsNullOrWhiteSpace(documento.Imovel.Titulo)
                        ? "Imóvel"
                        : documento.Imovel.Titulo;

                    var expiradoEm = documento.ValidoAte?.ToLocalTime().ToString("dd/MM/yyyy", PtBrCulture);
                    var mensagem = expiradoEm is null
                        ? $"O documento \"{documento.Descricao}\" do imóvel \"{tituloImovel}\" expirou."
                        : $"O documento \"{documento.Descricao}\" do imóvel \"{tituloImovel}\" expirou em {expiradoEm}.";

                    notificacoes.Add(new InAppNotification
                    {
                        UsuarioId = usuarioId,
                        Titulo = "Documento expirado",
                        Mensagem = mensagem,
                        LinkDestino = "/PortalProprietario/Home/Index",
                        Lida = false,
                        CreatedBy = SystemUser
                    });
                }
            }

            if (notificacoes.Count > 0)
            {
                context.Notificacoes.AddRange(notificacoes);
            }

            await context.SaveChangesAsync(cancellationToken);

            foreach (var auditoria in auditorias)
            {
                await auditTrail.RegisterAsync(
                    "PropertyDocument",
                    auditoria.DocumentId,
                    "AUTO_EXPIRE",
                    auditoria.Before,
                    auditoria.After,
                    SystemUser,
                    string.Empty,
                    string.Empty,
                    cancellationToken);
            }

            _logger.LogInformation(
                "{Quantidade} documentos marcados como expirados automaticamente. Notificações geradas: {Notificacoes}.",
                expirados.Count,
                notificacoes.Count);
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

    private static string CreateAuditSnapshot(PropertyDocument documento)
    {
        var payload = new
        {
            documento.Id,
            documento.ImovelId,
            documento.Descricao,
            documento.Status,
            documento.ValidoAte,
            documento.Versao,
            documento.UpdatedAt,
            documento.UpdatedBy
        };

        return JsonSerializer.Serialize(payload, AuditSerializerOptions);
    }

    private sealed record PendingAudit(Guid DocumentId, string Before, string After);
}
