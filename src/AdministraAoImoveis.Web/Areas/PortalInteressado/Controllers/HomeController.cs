using System.Linq;
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

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IFileStorageService fileStorageService,
        ILogger<HomeController> logger)
    {
        _context = context;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
        _logger = logger;
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
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var negotiation = await _context.Negociacoes
            .Include(n => n.Interessado)
            .FirstOrDefaultAsync(n => n.Id == input.NegociacaoId && n.Interessado != null && n.Interessado.UsuarioId == userId, cancellationToken);

        if (negotiation is null)
        {
            ModelState.AddModelError(nameof(ApplicantPortalMessageInputModel.NegociacaoId), "Negociação não encontrada.");
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, input);
            return View("Index", invalidModel ?? new ApplicantPortalViewModel());
        }

        var usuario = await _userManager.GetUserAsync(User);
        var autor = string.IsNullOrWhiteSpace(usuario?.NomeCompleto)
            ? usuario?.UserName ?? usuario?.Email ?? "Portal"
            : usuario!.NomeCompleto;

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
        var usuario = await _userManager.GetUserAsync(User);
        var autor = string.IsNullOrWhiteSpace(usuario?.NomeCompleto)
            ? usuario?.UserName ?? usuario?.Email ?? "Portal"
            : usuario!.NomeCompleto;

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

        _logger.LogInformation(
            "Documento {Categoria} v{Versao} enviado pelo interessado para a negociação {NegociacaoId}.",
            documento.Categoria,
            documento.Versao,
            negotiation.Id);

        TempData["Success"] = "Documento enviado com sucesso.";
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
        ApplicantDocumentUploadInputModel? upload = null)
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

        return new ApplicantPortalViewModel
        {
            Nome = applicant.Nome,
            Negociacoes = negociacoes,
            MensagensRecentes = mensagensRecentes,
            NovaMensagem = mensagemModel,
            Upload = uploadModel
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
}
