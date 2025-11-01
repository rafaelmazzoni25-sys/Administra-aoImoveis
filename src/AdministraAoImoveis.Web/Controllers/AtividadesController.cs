using System.Text.Json;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Infrastructure.FileStorage;
using AdministraAoImoveis.Web.Models;
using AdministraAoImoveis.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize(Roles = RoleNames.Operacional)]
public class AtividadesController : Controller
{
    private static readonly ActivityStatus[] FinalStatuses =
    {
        ActivityStatus.Concluida,
        ActivityStatus.Cancelada
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<AtividadesController> _logger;
    private readonly IAuditTrailService _auditTrailService;

    public AtividadesController(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        ILogger<AtividadesController> logger,
        IAuditTrailService auditTrailService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
        _auditTrailService = auditTrailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] ActivityStatus? status,
        [FromQuery] PriorityLevel? prioridade,
        [FromQuery] string? responsavel,
        CancellationToken cancellationToken)
    {
        var query = _context.Atividades
            .AsNoTracking()
            .Include(a => a.Comentarios)
            .Include(a => a.Anexos)
            .AsQueryable();

        if (status.HasValue)
        {
            var filtro = status.Value;
            query = query.Where(a => a.Status == filtro);
        }

        if (prioridade.HasValue)
        {
            var filtro = prioridade.Value;
            query = query.Where(a => a.Prioridade == filtro);
        }

        if (!string.IsNullOrWhiteSpace(responsavel))
        {
            query = query.Where(a => a.Responsavel == responsavel);
        }

        var atividades = await query
            .OrderByDescending(a => a.Prioridade)
            .ThenBy(a => a.DataLimite ?? DateTime.MaxValue)
            .ToListAsync(cancellationToken);

        var agora = DateTime.UtcNow;
        var itens = atividades
            .Select(a => BuildListItem(a, agora))
            .ToList();

        var totais = Enum.GetValues<ActivityStatus>()
            .ToDictionary(s => s, _ => 0);

        foreach (var item in itens)
        {
            totais[item.Activity.Status]++;
        }

        var model = new ActivityListViewModel
        {
            Status = status,
            Prioridade = prioridade,
            Responsavel = responsavel,
            Itens = itens,
            TotaisPorStatus = totais,
            Total = itens.Count,
            TotalAtrasadas = itens.Count(i => i.EstaAtrasada),
            TotalEmRisco = itens.Count(i => i.EmRisco)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Prioridades(CancellationToken cancellationToken)
    {
        var atividades = await _context.Atividades
            .AsNoTracking()
            .Where(a => !FinalStatuses.Contains(a.Status))
            .OrderByDescending(a => a.Prioridade)
            .ThenBy(a => a.DataLimite ?? DateTime.MaxValue)
            .ToListAsync(cancellationToken);

        var responsaveis = atividades
            .Select(a => a.Responsavel)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var mapa = Enum.GetValues<PriorityLevel>()
            .ToDictionary(
                prioridade => prioridade,
                prioridade => (IReadOnlyCollection<Activity>)atividades
                    .Where(a => a.Prioridade == prioridade)
                    .ToList());

        var model = new ActivityPriorityMatrixViewModel
        {
            Responsaveis = responsaveis,
            PorPrioridade = mapa,
            AtualizadoEm = DateTime.UtcNow
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var model = await LoadDetailModelAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = "Atualizacao")] ActivityUpdateInputModel input, CancellationToken cancellationToken)
    {
        var atividade = await _context.Atividades.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (atividade is null)
        {
            return NotFound();
        }

        var antes = JsonSerializer.Serialize(atividade);
        if (!ModelState.IsValid)
        {
            var invalidModel = await LoadDetailModelAsync(id, cancellationToken, input);
            return View("Details", invalidModel);
        }

        var usuario = User?.Identity?.Name ?? "Sistema";
        var erros = false;

        if (string.IsNullOrWhiteSpace(input.Responsavel))
        {
            ModelState.AddModelError(nameof(ActivityUpdateInputModel.Responsavel), "Responsável é obrigatório.");
            erros = true;
        }

        if (input.DataLimite.HasValue && !FinalStatuses.Contains(input.Status))
        {
            var prazoNormalizado = NormalizeToUtc(input.DataLimite.Value);
            if (prazoNormalizado < DateTime.UtcNow.AddHours(-1))
            {
                ModelState.AddModelError(nameof(ActivityUpdateInputModel.DataLimite), "Prazo não pode estar no passado para atividades ativas.");
                erros = true;
            }
        }

        if (erros)
        {
            var invalidModel = await LoadDetailModelAsync(id, cancellationToken, input);
            return View("Details", invalidModel);
        }

        var alteracoes = new List<string>();

        if (atividade.Status != input.Status)
        {
            alteracoes.Add($"Status: {atividade.Status} → {input.Status}");
            atividade.Status = input.Status;
        }

        if (atividade.Prioridade != input.Prioridade)
        {
            alteracoes.Add($"Prioridade: {atividade.Prioridade} → {input.Prioridade}");
            atividade.Prioridade = input.Prioridade;
        }

        var novoResponsavel = input.Responsavel?.Trim();
        if (!string.Equals(atividade.Responsavel, novoResponsavel, StringComparison.OrdinalIgnoreCase))
        {
            alteracoes.Add($"Responsável: {atividade.Responsavel} → {novoResponsavel}");
            atividade.Responsavel = novoResponsavel ?? string.Empty;
        }

        DateTime? dataLimiteNormalizada = null;
        if (input.DataLimite.HasValue)
        {
            dataLimiteNormalizada = NormalizeToUtc(input.DataLimite.Value);
        }

        if (atividade.DataLimite != dataLimiteNormalizada)
        {
            alteracoes.Add($"Prazo: {FormatDate(atividade.DataLimite)} → {FormatDate(dataLimiteNormalizada)}");
            atividade.DataLimite = dataLimiteNormalizada;
        }

        atividade.UpdatedAt = DateTime.UtcNow;
        atividade.UpdatedBy = usuario;

        if (alteracoes.Count > 0)
        {
            var logComentario = new ActivityComment
            {
                AtividadeId = atividade.Id,
                Autor = usuario,
                ComentadoEm = DateTime.UtcNow,
                Texto = string.Join(" | ", alteracoes)
            };
            _context.AtividadeComentarios.Add(logComentario);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(
            atividade.Id,
            "UPDATE",
            antes,
            JsonSerializer.Serialize(atividade),
            cancellationToken);

        if (alteracoes.Count > 0)
        {
            _logger.LogInformation(
                "Atividade {ActivityId} atualizada por {Usuario}: {Alteracoes}",
                atividade.Id,
                usuario,
                string.Join(", ", alteracoes));
        }
        else
        {
            _logger.LogInformation(
                "Atividade {ActivityId} recebeu atualização sem mudanças por {Usuario}",
                atividade.Id,
                usuario);
        }

        TempData["StatusMessage"] = alteracoes.Count == 0
            ? "Nenhuma alteração detectada na atividade."
            : "Atividade atualizada com sucesso.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarResponsavel([FromBody] ActivityAssignmentRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.Id == Guid.Empty)
        {
            return BadRequest(new { erro = "Solicitação inválida." });
        }

        var novoResponsavel = request.NovoResponsavel;

        var atividade = await _context.Atividades.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (atividade is null)
        {
            return NotFound();
        }

        var antes = JsonSerializer.Serialize(atividade);
        var normalizado = novoResponsavel?.Trim() ?? string.Empty;
        if (string.Equals(atividade.Responsavel, normalizado, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { mensagem = "Responsável já atribuído." });
        }

        var anterior = atividade.Responsavel;
        atividade.Responsavel = normalizado;
        atividade.UpdatedAt = DateTime.UtcNow;
        atividade.UpdatedBy = User?.Identity?.Name ?? "Sistema";

        _context.AtividadeComentarios.Add(new ActivityComment
        {
            AtividadeId = atividade.Id,
            Autor = atividade.UpdatedBy,
            ComentadoEm = DateTime.UtcNow,
            Texto = $"Responsável alterado: {anterior} → {normalizado}"
        });

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(
            atividade.Id,
            "REASSIGN",
            antes,
            JsonSerializer.Serialize(atividade),
            cancellationToken);

        return Ok(new { mensagem = "Responsável atualizado." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, ActivityCommentInputModel input, CancellationToken cancellationToken)
    {
        var atividade = await _context.Atividades.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (atividade is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await LoadDetailModelAsync(id, cancellationToken);
            return View("Details", invalidModel);
        }

        var comentario = new ActivityComment
        {
            AtividadeId = id,
            Texto = input.Texto.Trim(),
            Autor = User?.Identity?.Name ?? "Usuário",
            ComentadoEm = DateTime.UtcNow
        };

        _context.AtividadeComentarios.Add(comentario);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(
            atividade.Id,
            "COMMENT_ADD",
            string.Empty,
            JsonSerializer.Serialize(new
            {
                comentario.Id,
                comentario.Autor,
                comentario.Texto,
                comentario.ComentadoEm
            }),
            cancellationToken);

        _logger.LogInformation(
            "Comentário adicionado à atividade {ActivityId} por {Autor}",
            id,
            comentario.Autor);

        TempData["StatusMessage"] = "Comentário registrado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(Guid id, ActivityAttachmentInputModel input, CancellationToken cancellationToken)
    {
        var atividade = await _context.Atividades.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (atividade is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid || input.Arquivo is null || input.Arquivo.Length == 0)
        {
            ModelState.AddModelError(nameof(ActivityAttachmentInputModel.Arquivo), "Arquivo inválido.");
            var invalidModel = await LoadDetailModelAsync(id, cancellationToken);
            return View("Details", invalidModel);
        }

        await using var stream = input.Arquivo.OpenReadStream();
        var storedFile = await _fileStorageService.SaveAsync(
            input.Arquivo.FileName,
            input.Arquivo.ContentType,
            stream,
            category: "atividades",
            cancellationToken);

        _context.Arquivos.Add(storedFile);

        var attachment = new ActivityAttachment
        {
            AtividadeId = atividade.Id,
            ArquivoId = storedFile.Id,
            Arquivo = storedFile
        };

        _context.AtividadeAnexos.Add(attachment);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(
            atividade.Id,
            "ATTACHMENT_ADD",
            string.Empty,
            JsonSerializer.Serialize(new
            {
                attachment.Id,
                Arquivo = storedFile.NomeOriginal,
                ArquivoId = storedFile.Id
            }),
            cancellationToken);

        _logger.LogInformation(
            "Anexo {FileName} salvo para a atividade {ActivityId}",
            storedFile.NomeOriginal,
            atividade.Id);

        TempData["StatusMessage"] = "Anexo enviado com sucesso.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await _context.AtividadeAnexos
            .AsNoTracking()
            .Include(a => a.Arquivo)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.AtividadeId == id, cancellationToken);

        if (attachment?.Arquivo is null)
        {
            return NotFound();
        }

        var stream = await _fileStorageService.OpenAsync(attachment.Arquivo, cancellationToken);
        return File(stream, attachment.Arquivo.ConteudoTipo, attachment.Arquivo.NomeOriginal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await _context.AtividadeAnexos
            .Include(a => a.Arquivo)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.AtividadeId == id, cancellationToken);

        if (attachment?.Arquivo is null)
        {
            return NotFound();
        }

        var detalhes = new
        {
            attachment.Id,
            Arquivo = attachment.Arquivo.NomeOriginal,
            attachment.ArquivoId
        };

        await _fileStorageService.DeleteAsync(attachment.Arquivo, cancellationToken);
        _context.Arquivos.Remove(attachment.Arquivo);
        _context.AtividadeAnexos.Remove(attachment);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(
            id,
            "ATTACHMENT_REMOVE",
            JsonSerializer.Serialize(detalhes),
            string.Empty,
            cancellationToken);

        _logger.LogInformation(
            "Anexo {AttachmentId} removido da atividade {ActivityId} por {Usuario}",
            attachment.Id,
            id,
            User?.Identity?.Name ?? "Sistema");

        TempData["StatusMessage"] = "Anexo removido.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<ActivityDetailViewModel?> LoadDetailModelAsync(
        Guid id,
        CancellationToken cancellationToken,
        ActivityUpdateInputModel? updateOverride = null)
    {
        var atividade = await _context.Atividades
            .AsNoTracking()
            .Include(a => a.Comentarios)
            .Include(a => a.Anexos)
                .ThenInclude(a => a.Arquivo)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (atividade is null)
        {
            return null;
        }

        return BuildDetailViewModel(atividade, DateTime.UtcNow, updateOverride);
    }

    private static ActivityListItemViewModel BuildListItem(Activity atividade, DateTime agora)
    {
        var metrics = CalculateMetrics(atividade, agora);
        return new ActivityListItemViewModel
        {
            Activity = atividade,
            EstaAtrasada = metrics.Overdue,
            EmRisco = metrics.Risk,
            TempoRestante = metrics.Remaining,
            PercentualSlaConsumido = metrics.Percent,
            Comentarios = atividade.Comentarios.Count,
            Anexos = atividade.Anexos.Count
        };
    }

    private static ActivityDetailViewModel BuildDetailViewModel(
        Activity atividade,
        DateTime agora,
        ActivityUpdateInputModel? updateOverride)
    {
        var metrics = CalculateMetrics(atividade, agora);

        var updateModel = updateOverride ?? new ActivityUpdateInputModel
        {
            Status = atividade.Status,
            Prioridade = atividade.Prioridade,
            Responsavel = atividade.Responsavel,
            DataLimite = atividade.DataLimite?.ToLocalTime()
        };

        return new ActivityDetailViewModel
        {
            Activity = atividade,
            Comentarios = atividade.Comentarios
                .OrderByDescending(c => c.ComentadoEm)
                .ToArray(),
            Anexos = atividade.Anexos
                .OrderByDescending(a => a.CreatedAt)
                .ToArray(),
            EstaAtrasada = metrics.Overdue,
            EmRisco = metrics.Risk,
            TempoRestante = metrics.Remaining,
            PercentualSlaConsumido = metrics.Percent,
            Atualizacao = updateModel
        };
    }

    private static ActivityMetrics CalculateMetrics(Activity atividade, DateTime agora)
    {
        if (atividade.DataLimite is null)
        {
            return new ActivityMetrics(false, false, null, 0);
        }

        var prazoUtc = NormalizeToUtc(atividade.DataLimite.Value);
        var remaining = prazoUtc - agora;
        var overdue = remaining < TimeSpan.Zero && !IsFinalStatus(atividade.Status);
        var risk = !overdue && !IsFinalStatus(atividade.Status) && remaining <= TimeSpan.FromHours(24);

        var totalWindow = prazoUtc - atividade.CreatedAt;
        double percent;
        if (totalWindow.TotalMinutes <= 0)
        {
            percent = overdue ? 100 : 0;
        }
        else
        {
            var elapsed = agora - atividade.CreatedAt;
            percent = Math.Clamp(elapsed.TotalMinutes / totalWindow.TotalMinutes * 100, 0, 150);
        }

        return new ActivityMetrics(overdue, risk, remaining, percent);
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime(),
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => value
        };
    }

    private static bool IsFinalStatus(ActivityStatus status) => FinalStatuses.Contains(status);

    private static string FormatDate(DateTime? date)
    {
        return date.HasValue ? date.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "sem prazo";
    }

    private async Task RegistrarAuditoriaAsync(Guid atividadeId, string operacao, string antes, string depois, CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync(
            "Activity",
            atividadeId,
            operacao,
            antes,
            depois,
            usuario,
            ip,
            host,
            cancellationToken);
    }

    private readonly record struct ActivityMetrics(bool Overdue, bool Risk, TimeSpan? Remaining, double Percent);
}
