using System.Linq;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AdministraAoImoveis.Web.Areas.PortalProprietario.Controllers;

[Area("PortalProprietario")]
[Authorize(Roles = "PROPRIETARIO")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<HomeController> logger)
    {
        _context = context;
        _userManager = userManager;
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
        return View(model ?? new OwnerPortalViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostMensagem(OwnerPortalMessageInputModel input, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, input);
            return View("Index", invalidModel ?? new OwnerPortalViewModel());
        }

        var imovel = await _context.Imoveis
            .Include(i => i.Proprietario)
            .FirstOrDefaultAsync(i => i.Id == input.ImovelId && i.Proprietario != null && i.Proprietario.UsuarioId == userId, cancellationToken);

        if (imovel is null)
        {
            ModelState.AddModelError(nameof(OwnerPortalMessageInputModel.ImovelId), "Imóvel selecionado não foi encontrado.");
            var invalidModel = await BuildDashboardAsync(userId, cancellationToken, input);
            return View("Index", invalidModel ?? new OwnerPortalViewModel());
        }

        var usuario = await _userManager.GetUserAsync(User);
        var autor = string.IsNullOrWhiteSpace(usuario?.NomeCompleto)
            ? usuario?.UserName ?? usuario?.Email ?? "Portal"
            : usuario!.NomeCompleto;

        var mensagem = new ContextMessage
        {
            ContextoTipo = ActivityLinkType.Imovel,
            ContextoId = imovel.Id,
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
            "Mensagem registrada no portal do proprietário para o imóvel {ImovelId} por {Usuario}.",
            imovel.Id,
            autor);

        TempData["Success"] = "Mensagem enviada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<OwnerPortalViewModel?> BuildDashboardAsync(
        string userId,
        CancellationToken cancellationToken,
        OwnerPortalMessageInputModel? mensagem = null)
    {
        var owner = await _context.Proprietarios
            .AsNoTracking()
            .Include(p => p.Imoveis)
                .ThenInclude(i => i.Vistorias)
            .Include(p => p.Imoveis)
                .ThenInclude(i => i.Manutencoes)
            .Include(p => p.Imoveis)
                .ThenInclude(i => i.Negociacoes)
                    .ThenInclude(n => n.Interessado)
            .Include(p => p.Imoveis)
                .ThenInclude(i => i.Negociacoes)
                    .ThenInclude(n => n.LancamentosFinanceiros)
            .Include(p => p.Imoveis)
                .ThenInclude(i => i.Atividades)
            .Include(p => p.Imoveis)
                .ThenInclude(i => i.Documentos)
            .FirstOrDefaultAsync(p => p.UsuarioId == userId, cancellationToken);

        if (owner is null)
        {
            return null;
        }

        var propriedades = owner.Imoveis
            .OrderBy(i => i.CodigoInterno)
            .Select(MapProperty)
            .ToList();

        var propertyLookup = propriedades.ToDictionary(p => p.Id, p => $"{p.CodigoInterno} - {p.Titulo}");
        var mensagensRecentes = await LoadMessagesAsync(propertyLookup, cancellationToken);

        var documentosPendentes = owner.Imoveis
            .SelectMany(i => i.Documentos)
            .Where(d => d.RequerAceiteProprietario && (d.Status == DocumentStatus.Pendente || d.Status == DocumentStatus.Expirado))
            .OrderBy(d => d.Imovel?.CodigoInterno)
            .ThenByDescending(d => d.Versao)
            .Select(d => new OwnerDocumentSummaryViewModel
            {
                DocumentoId = d.Id,
                ImovelId = d.ImovelId,
                Imovel = d.Imovel is null
                    ? "Imóvel"
                    : $"{d.Imovel.CodigoInterno} - {d.Imovel.Titulo}",
                Descricao = d.Descricao,
                Versao = d.Versao,
                Status = d.Status,
                ValidoAte = d.ValidoAte,
                RequerAceite = d.RequerAceiteProprietario
            })
            .ToList();

        var metricas = new OwnerPortalMetricsViewModel
        {
            TotalImoveis = propriedades.Count,
            Disponiveis = propriedades.Count(p => p.Status == AvailabilityStatus.Disponivel),
            EmNegociacao = propriedades.Count(p => p.Status is AvailabilityStatus.Reservado or AvailabilityStatus.EmNegociacao),
            EmManutencao = propriedades.Count(p => p.Status == AvailabilityStatus.EmManutencao),
            VistoriasPendentes = propriedades.Sum(p => p.ProximasVistorias.Count),
            ManutencoesAbertas = propriedades.Sum(p => p.ManutencoesEmAberto.Count),
            PendenciasCriticas = propriedades.Sum(p => p.PendenciasCriticas),
            DocumentosPendentes = documentosPendentes.Count
        };

        return new OwnerPortalViewModel
        {
            Nome = owner.Nome,
            Metricas = metricas,
            Imoveis = propriedades,
            DocumentosPendentes = documentosPendentes,
            MensagensRecentes = mensagensRecentes,
            NovaMensagem = mensagem ?? new OwnerPortalMessageInputModel(),
            PodeEnviarMensagem = propriedades.Any()
        };
    }

    private async Task<IReadOnlyCollection<PortalMessageViewModel>> LoadMessagesAsync(
        IReadOnlyDictionary<Guid, string> propertyLookup,
        CancellationToken cancellationToken)
    {
        if (propertyLookup.Count == 0)
        {
            return Array.Empty<PortalMessageViewModel>();
        }

        var propertyIds = propertyLookup.Keys.ToArray();

        var mensagens = await _context.Mensagens
            .AsNoTracking()
            .Where(m => m.ContextoTipo == ActivityLinkType.Imovel && propertyIds.Contains(m.ContextoId))
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
                Contexto = propertyLookup.TryGetValue(m.ContextoId, out var contexto) ? contexto : "Imóvel",
                Autor = string.IsNullOrWhiteSpace(m.Autor) ? "Usuário" : m.Autor!,
                EnviadaEm = m.EnviadaEm,
                Conteudo = m.Mensagem
            })
            .ToList();
    }

    private static OwnerPortalPropertyViewModel MapProperty(Property property)
    {
        var endereco = string.Join(" ", new[]
        {
            property.Endereco,
            string.IsNullOrWhiteSpace(property.Bairro) ? null : $"- {property.Bairro}",
            $"- {property.Cidade}/{property.Estado}"
        }.Where(p => !string.IsNullOrWhiteSpace(p)));

        var proximasVistorias = property.Vistorias
            .Where(v => v.Status != InspectionStatus.Concluida)
            .OrderBy(v => v.AgendadaPara)
            .Take(3)
            .Select(v => new OwnerPortalInspectionSummary
            {
                Id = v.Id,
                Status = v.Status,
                Tipo = v.Tipo,
                AgendadaPara = v.AgendadaPara,
                Responsavel = v.Responsavel
            })
            .ToList();

        var manutencoesAbertas = property.Manutencoes
            .Where(m => m.Status is not MaintenanceOrderStatus.Concluida and not MaintenanceOrderStatus.Cancelada)
            .OrderBy(m => m.PrevisaoConclusao ?? DateTime.MaxValue)
            .Take(3)
            .Select(m => new OwnerPortalMaintenanceSummary
            {
                Id = m.Id,
                Titulo = m.Titulo,
                Status = m.Status,
                PrevisaoConclusao = m.PrevisaoConclusao,
                Responsavel = m.Responsavel
            })
            .ToList();

        var negociacoesAtivas = property.Negociacoes
            .Where(n => n.Ativa)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new OwnerPortalNegotiationSummary
            {
                Id = n.Id,
                Etapa = n.Etapa,
                CriadaEm = n.CreatedAt,
                ReservadoAte = n.ReservadoAte,
                ValorSinal = n.ValorSinal,
                Interessado = n.Interessado?.Nome ?? string.Empty,
                TotalPrevisto = n.LancamentosFinanceiros.Sum(l => l.Valor),
                TotalRecebido = n.LancamentosFinanceiros.Where(l => l.Status == FinancialStatus.Recebido).Sum(l => l.Valor),
                TotalPendente = n.LancamentosFinanceiros.Where(l => l.Status == FinancialStatus.Pendente).Sum(l => l.Valor)
            })
            .ToList();

        var pendenciasCriticas = property.Atividades
            .Count(a => a.Status is not ActivityStatus.Concluida and not ActivityStatus.Cancelada && a.Prioridade == PriorityLevel.Critica);

        return new OwnerPortalPropertyViewModel
        {
            Id = property.Id,
            CodigoInterno = property.CodigoInterno,
            Titulo = property.Titulo,
            Endereco = endereco,
            Status = property.StatusDisponibilidade,
            DisponivelEm = property.DataPrevistaDisponibilidade,
            PendenciasCriticas = pendenciasCriticas,
            ProximasVistorias = proximasVistorias,
            ManutencoesEmAberto = manutencoesAbertas,
            NegociacoesAtivas = negociacoesAtivas
        };
    }
}
