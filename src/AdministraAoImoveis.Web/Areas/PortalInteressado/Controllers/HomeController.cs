using System;
using System.Linq;
using System.Text.Json;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Infrastructure.FileStorage;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AdministraAoImoveis.Web.Services;

namespace AdministraAoImoveis.Web.Areas.PortalInteressado.Controllers;

[Area("PortalInteressado")]
[Authorize(Roles = "INTERESSADO")]
public class HomeController : Controller
{
    private const string StorageCategory = "negotiation-documents";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<HomeController> _logger;
    private readonly IAuditTrailService _auditTrailService;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IFileStorageService fileStorageService,
        ILogger<HomeController> logger,
        IAuditTrailService auditTrailService)
    {
        _context = context;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
        _logger = logger;
        _auditTrailService = auditTrailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var model = await BuildDashboardAsync(userId, cancellationToken);
        return View(model ?? new ApplicantPortalViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostMensagem(ApplicantPortalMessageInputModel input, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, mensagem: input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var negotiation = await _context.Negociacoes
            .Include(n => n.Interessado)
            .FirstOrDefaultAsync(n => n.Id == input.NegociacaoId && n.Interessado != null && n.Interessado.UsuarioId == userId, cancellationToken);

        if (negotiation is null)
        {
            ModelState.AddModelError(nameof(ApplicantPortalMessageInputModel.NegociacaoId), "Negociação não encontrada.");
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, mensagem: input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var autor = await GetCurrentUserDisplayNameAsync();

        var mensagem = new ContextMessage
        {
            ContextoTipo = ActivityLinkType.Negociacao,
            ContextoId = negotiation.Id,
            UsuarioId = userId,
            Mensagem = input.Mensagem.Trim(),
            EnviadaEm = DateTime.UtcNow,
            CreatedBy = autor,
            UpdatedBy = autor,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Mensagens.Add(mensagem);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(
            "ContextMessage",
            mensagem.Id,
            "CREATE",
            string.Empty,
            SerializeMensagem(mensagem),
            cancellationToken);

        _logger.LogInformation(
            "Mensagem registrada no portal do interessado para a negociação {NegociacaoId} por {Usuario}.",
            negotiation.Id,
            autor);

        TempData["Success"] = "Mensagem enviada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocumento(ApplicantDocumentUploadInputModel input, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, upload: input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        if (input.Arquivo is null)
        {
            ModelState.AddModelError(nameof(ApplicantDocumentUploadInputModel.Arquivo), "Selecione um arquivo válido.");
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, upload: input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var negotiation = await _context.Negociacoes
            .Include(n => n.Interessado)
            .Include(n => n.Documentos)
            .FirstOrDefaultAsync(n => n.Id == input.NegociacaoId && n.Interessado != null && n.Interessado.UsuarioId == userId, cancellationToken);

        if (negotiation is null)
        {
            ModelState.AddModelError(nameof(ApplicantDocumentUploadInputModel.NegociacaoId), "Negociação não encontrada.");
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, upload: input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var categoria = input.Categoria.Trim();
        var autor = await GetCurrentUserDisplayNameAsync();

        await using var content = input.Arquivo.OpenReadStream();
        var storedFile = await _fileStorageService.SaveAsync(
            input.Arquivo.FileName,
            input.Arquivo.ContentType,
            content,
            StorageCategory,
            cancellationToken);

        storedFile.CreatedBy = autor;
        _context.Arquivos.Add(storedFile);

        var existingVersions = negotiation.Documentos
            .Where(d => string.Equals(d.Categoria, categoria, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var versao = existingVersions.Any()
            ? existingVersions.Max(d => d.Versao) + 1
            : 1;

        var documento = new NegotiationDocument
        {
            NegociacaoId = negotiation.Id,
            ArquivoId = storedFile.Id,
            Arquivo = storedFile,
            Categoria = categoria,
            Versao = versao,
            CreatedBy = autor,
            UpdatedBy = autor,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NegotiationDocuments.Add(documento);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(
            "NegotiationDocument",
            documento.Id,
            "CREATE",
            string.Empty,
            SerializeNegotiationDocument(documento),
            cancellationToken);

        _logger.LogInformation(
            "Documento {Categoria} v{Versao} enviado pelo interessado para a negociação {NegociacaoId}.",
            documento.Categoria,
            documento.Versao,
            negotiation.Id);

        TempData["Success"] = "Documento enviado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgendarVisita(ApplicantVisitScheduleInputModel input, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, agendamento: input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var negotiation = await _context.Negociacoes
            .Include(n => n.Interessado)
            .Include(n => n.Imovel)
            .FirstOrDefaultAsync(n => n.Id == input.NegociacaoId && n.Interessado != null && n.Interessado.UsuarioId == userId, cancellationToken);

        if (negotiation is null)
        {
            ModelState.AddModelError(nameof(ApplicantVisitScheduleInputModel.NegociacaoId), "Negociação não encontrada.");
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, agendamento: input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var dataHora = input.DataHora!.Value;
        if (dataHora.Kind == DateTimeKind.Unspecified)
        {
            dataHora = DateTime.SpecifyKind(dataHora, DateTimeKind.Local);
        }

        var inicio = dataHora.Kind == DateTimeKind.Utc ? dataHora : dataHora.ToUniversalTime();
        if (inicio < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(ApplicantVisitScheduleInputModel.DataHora), "Escolha uma data futura para a visita.");
        }

        var fim = inicio.AddHours(1);
        var responsavel = negotiation.CreatedBy?.Trim();

        var conflitos = await _context.Agenda
            .Where(e => e.Inicio < fim && e.Fim > inicio)
            .Where(e => e.ImovelId == negotiation.ImovelId || (!string.IsNullOrWhiteSpace(responsavel) && string.Equals(e.Responsavel, responsavel, StringComparison.OrdinalIgnoreCase)))
            .ToListAsync(cancellationToken);

        if (conflitos.Any(e => e.ImovelId == negotiation.ImovelId))
        {
            ModelState.AddModelError(nameof(ApplicantVisitScheduleInputModel.DataHora), "Já existe compromisso para este imóvel no horário selecionado.");
        }

        if (!string.IsNullOrWhiteSpace(responsavel) && conflitos.Any(e => string.Equals(e.Responsavel, responsavel, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(ApplicantVisitScheduleInputModel.DataHora), "O responsável pela negociação já possui outro compromisso neste horário.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, agendamento: input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var solicitante = await GetCurrentUserDisplayNameAsync();
        var observacoes = input.Observacoes?.Trim() ?? string.Empty;

        var agendaEntry = new ScheduleEntry
        {
            Titulo = $"Visita - {negotiation.Imovel?.CodigoInterno ?? "Imóvel"}",
            Tipo = "Visita",
            Setor = "Comercial",
            Inicio = inicio,
            Fim = fim,
            Responsavel = responsavel ?? string.Empty,
            ImovelId = negotiation.ImovelId,
            NegociacaoId = negotiation.Id,
            Observacoes = observacoes,
            CreatedBy = solicitante,
            UpdatedBy = solicitante,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Agenda.Add(agendaEntry);

        var evento = new NegotiationEvent
        {
            NegociacaoId = negotiation.Id,
            Titulo = "Visita agendada pelo interessado",
            Descricao = $"Visita marcada para {inicio.ToLocalTime():dd/MM/yyyy HH:mm}.",
            Responsavel = solicitante,
            OcorridoEm = DateTime.UtcNow,
            CreatedBy = solicitante,
            UpdatedBy = solicitante,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NegociacaoEventos.Add(evento);

        await NotificarResponsavelAsync(agendaEntry, solicitante, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(
            "ScheduleEntry",
            agendaEntry.Id,
            "CREATE",
            string.Empty,
            SerializeScheduleEntry(agendaEntry),
            cancellationToken);

        await RegistrarAuditoriaAsync(
            "NegotiationEvent",
            evento.Id,
            "CREATE",
            string.Empty,
            SerializeNegotiationEvent(evento),
            cancellationToken);

        _logger.LogInformation(
            "Visita solicitada pelo interessado {Usuario} para a negociação {NegociacaoId} em {DataHora}.",
            solicitante,
            negotiation.Id,
            inicio);

        TempData["Success"] = "Visita solicitada com sucesso. A equipe entrará em contato para confirmar.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("documentos/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocumento(Guid documentId, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var documento = await _context.NegotiationDocuments
            .Include(d => d.Arquivo)
            .Include(d => d.Negociacao)!
                .ThenInclude(n => n.Interessado)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Negociacao != null && d.Negociacao.Interessado != null && d.Negociacao.Interessado.UsuarioId == userId, cancellationToken);

        if (documento is null || documento.Arquivo is null)
        {
            return NotFound();
        }

        var stream = await _fileStorageService.OpenAsync(documento.Arquivo, cancellationToken);
        await RegistrarAuditoriaAsync(
            "NegotiationDocument",
            documento.Id,
            "DOWNLOAD",
            string.Empty,
            string.Empty,
            cancellationToken);
        _logger.LogInformation(
            "Documento {DocumentoId} baixado pelo interessado vinculado à negociação {NegociacaoId}.",
            documento.Id,
            documento.NegociacaoId);

        return File(stream, documento.Arquivo.ConteudoTipo, documento.Arquivo.NomeOriginal);
    }

    private async Task<ApplicantPortalViewModel?> BuildDashboardAsync(
        string userId,
        CancellationToken cancellationToken,
        ApplicantPortalMessageInputModel? mensagem = null,
        ApplicantDocumentUploadInputModel? upload = null,
        ApplicantVisitScheduleInputModel? agendamento = null)
    {
        var applicant = await _context.Interessados
            .AsNoTracking()
            .Include(i => i.Negociacoes)
                .ThenInclude(n => n.Imovel)
            .Include(i => i.Negociacoes)
                .ThenInclude(n => n.Eventos)
            .Include(i => i.Negociacoes)
                .ThenInclude(n => n.Documentos)
                    .ThenInclude(d => d.Arquivo)
            .Include(i => i.Negociacoes)
                .ThenInclude(n => n.LancamentosFinanceiros)
            .FirstOrDefaultAsync(i => i.UsuarioId == userId, cancellationToken);

        if (applicant is null)
        {
            return null;
        }

        var negociacoes = applicant.Negociacoes
            .OrderByDescending(n => n.CreatedAt)
            .Select(MapNegotiation)
            .ToList();

        var negotiationLookup = negociacoes.ToDictionary(n => n.Id, n => n.Imovel);
        var mensagensRecentes = await LoadMessagesAsync(negotiationLookup, cancellationToken);

        var mensagemModel = mensagem is null
            ? new ApplicantPortalMessageInputModel()
            : new ApplicantPortalMessageInputModel
            {
                NegociacaoId = mensagem.NegociacaoId,
                Mensagem = mensagem.Mensagem
            };

        var uploadModel = upload is null
            ? new ApplicantDocumentUploadInputModel()
            : new ApplicantDocumentUploadInputModel
            {
                NegociacaoId = upload.NegociacaoId,
                Categoria = upload.Categoria
            };

        var agendamentoModel = agendamento is null
            ? new ApplicantVisitScheduleInputModel()
            : new ApplicantVisitScheduleInputModel
            {
                NegociacaoId = agendamento.NegociacaoId,
                DataHora = agendamento.DataHora,
                Observacoes = agendamento.Observacoes
            };

        return new ApplicantPortalViewModel
        {
            Nome = applicant.Nome,
            Negociacoes = negociacoes,
            MensagensRecentes = mensagensRecentes,
            NovaMensagem = mensagemModel,
            Upload = uploadModel,
            AgendamentoVisita = agendamentoModel
        };
    }

    private async Task<IReadOnlyCollection<PortalMessageViewModel>> LoadMessagesAsync(
        IReadOnlyDictionary<Guid, string> negotiationLookup,
        CancellationToken cancellationToken)
    {
        if (negotiationLookup.Count == 0)
        {
            return Array.Empty<PortalMessageViewModel>();
        }

        var negotiationIds = negotiationLookup.Keys.ToArray();

        var mensagens = await _context.Mensagens
            .AsNoTracking()
            .Where(m => m.ContextoTipo == ActivityLinkType.Negociacao && negotiationIds.Contains(m.ContextoId))
            .OrderByDescending(m => m.EnviadaEm)
            .Take(20)
            .Select(m => new
            {
                m.Id,
                m.ContextoId,
                m.Mensagem,
                m.EnviadaEm,
                Autor = _context.Users
                    .Where(u => u.Id == m.UsuarioId)
                    .Select(u => string.IsNullOrWhiteSpace(u.NomeCompleto)
                        ? u.Email ?? u.UserName ?? "Usuário"
                        : u.NomeCompleto)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return mensagens
            .Select(m => new PortalMessageViewModel
            {
                Id = m.Id,
                Contexto = negotiationLookup.TryGetValue(m.ContextoId, out var contexto) ? contexto : "Negociação",
                Autor = string.IsNullOrWhiteSpace(m.Autor) ? "Usuário" : m.Autor!,
                EnviadaEm = m.EnviadaEm,
                Conteudo = m.Mensagem
            })
            .ToList();
    }

    private async Task NotificarResponsavelAsync(ScheduleEntry entrada, string solicitante, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entrada.Responsavel))
        {
            return;
        }

        var usuario = await _userManager.FindByNameAsync(entrada.Responsavel);
        if (usuario is null)
        {
            return;
        }

        var notificacao = new InAppNotification
        {
            UsuarioId = usuario.Id,
            Titulo = "Visita agendada pelo interessado",
            Mensagem = $"{entrada.Titulo} em {entrada.Inicio.ToLocalTime():dd/MM/yyyy HH:mm}",
            LinkDestino = Url.Action("Index", "Agenda", new { area = string.Empty }) ?? string.Empty,
            Lida = false,
            CreatedBy = solicitante
        };

        _context.Notificacoes.Add(notificacao);
    }

    private async Task<string> GetCurrentUserDisplayNameAsync()
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return "Portal";
        }

        return string.IsNullOrWhiteSpace(usuario.NomeCompleto)
            ? usuario.UserName ?? usuario.Email ?? "Portal"
            : usuario.NomeCompleto;
    }

    private static ApplicantNegotiationViewModel MapNegotiation(Negotiation negotiation)
    {
        var imovelNome = negotiation.Imovel is null
            ? "Imóvel não informado"
            : $"{negotiation.Imovel.CodigoInterno} - {negotiation.Imovel.Titulo}";

        var endereco = negotiation.Imovel is null
            ? string.Empty
            : string.Join(" ", new[]
            {
                negotiation.Imovel.Endereco,
                string.IsNullOrWhiteSpace(negotiation.Imovel.Bairro) ? null : $"- {negotiation.Imovel.Bairro}",
                $"- {negotiation.Imovel.Cidade}/{negotiation.Imovel.Estado}"
            }.Where(p => !string.IsNullOrWhiteSpace(p)));

        var timeline = negotiation.Eventos
            .OrderByDescending(e => e.OcorridoEm)
            .Select(e => new PortalTimelineEntryViewModel
            {
                Titulo = e.Titulo,
                Descricao = e.Descricao,
                OcorridoEm = e.OcorridoEm,
                Responsavel = e.Responsavel
            })
            .ToList();

        var lancamentos = negotiation.LancamentosFinanceiros
            .OrderBy(l => l.DataPrevista ?? l.CreatedAt)
            .Select(l => new ApplicantFinancialSummaryViewModel
            {
                Id = l.Id,
                TipoLancamento = l.TipoLancamento,
                Valor = l.Valor,
                Status = l.Status,
                DataPrevista = l.DataPrevista,
                DataEfetivacao = l.DataEfetivacao
            })
            .ToList();

        var documentos = negotiation.Documentos
            .OrderByDescending(d => d.Versao)
            .Select(d => new ApplicantDocumentViewModel
            {
                Id = d.Id,
                Categoria = d.Categoria,
                Versao = d.Versao,
                NomeArquivo = d.Arquivo?.NomeOriginal ?? "Documento"
            })
            .ToList();

        return new ApplicantNegotiationViewModel
        {
            Id = negotiation.Id,
            Imovel = imovelNome,
            Endereco = endereco,
            Etapa = negotiation.Etapa,
            Ativa = negotiation.Ativa,
            CriadaEm = negotiation.CreatedAt,
            ReservadoAte = negotiation.ReservadoAte,
            ValorSinal = negotiation.ValorSinal,
            Timeline = timeline,
            Lancamentos = lancamentos,
            Documentos = documentos
        };
    }

    private async Task RegistrarAuditoriaAsync(
        string entidade,
        Guid entidadeId,
        string operacao,
        string antes,
        string depois,
        CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Portal";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync(
            entidade,
            entidadeId,
            operacao,
            antes,
            depois,
            usuario,
            ip,
            host,
            cancellationToken);
    }

    private static string SerializeMensagem(ContextMessage mensagem)
    {
        var payload = new
        {
            mensagem.Id,
            mensagem.ContextoTipo,
            mensagem.ContextoId,
            mensagem.UsuarioId,
            mensagem.Mensagem,
            mensagem.EnviadaEm,
            mensagem.CreatedAt,
            mensagem.CreatedBy
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeNegotiationDocument(NegotiationDocument documento)
    {
        var payload = new
        {
            documento.Id,
            documento.NegociacaoId,
            documento.Categoria,
            documento.Versao,
            documento.ArquivoId,
            documento.CreatedAt,
            documento.CreatedBy
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeScheduleEntry(ScheduleEntry entrada)
    {
        var payload = new
        {
            entrada.Id,
            entrada.Titulo,
            entrada.Tipo,
            entrada.Setor,
            entrada.Inicio,
            entrada.Fim,
            entrada.Responsavel,
            entrada.ImovelId,
            entrada.NegociacaoId,
            entrada.Observacoes,
            entrada.CreatedAt,
            entrada.CreatedBy
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeNegotiationEvent(NegotiationEvent evento)
    {
        var payload = new
        {
            evento.Id,
            evento.NegociacaoId,
            evento.Titulo,
            evento.Descricao,
            evento.Responsavel,
            evento.OcorridoEm,
            evento.CreatedAt,
            evento.CreatedBy
        };
    }

    private async Task<IReadOnlyCollection<PortalMessageViewModel>> LoadMessagesAsync(
        IReadOnlyDictionary<Guid, string> negotiationLookup,
        CancellationToken cancellationToken)
    {
        if (negotiationLookup.Count == 0)
        {
            return Array.Empty<PortalMessageViewModel>();
        }

        var negotiationIds = negotiationLookup.Keys.ToArray();

        return JsonSerializer.Serialize(payload);
    }
}
