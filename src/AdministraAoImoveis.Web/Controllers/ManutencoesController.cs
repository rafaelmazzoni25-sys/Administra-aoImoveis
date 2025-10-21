using System;
using System.Linq;
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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize(Roles = RoleNames.ManutencaoEquipe)]
public class ManutencoesController : Controller
{
    private static readonly MaintenanceOrderStatus[] FinalStatuses =
    {
        MaintenanceOrderStatus.Concluida,
        MaintenanceOrderStatus.Cancelada
    };

    private static readonly IReadOnlyDictionary<MaintenanceOrderStatus, MaintenanceOrderStatus[]> AllowedTransitions = new Dictionary<MaintenanceOrderStatus, MaintenanceOrderStatus[]>
    {
        [MaintenanceOrderStatus.Solicitada] = new[] { MaintenanceOrderStatus.Aprovada, MaintenanceOrderStatus.Cancelada },
        [MaintenanceOrderStatus.Aprovada] = new[] { MaintenanceOrderStatus.EmExecucao, MaintenanceOrderStatus.Cancelada },
        [MaintenanceOrderStatus.EmExecucao] = new[] { MaintenanceOrderStatus.Concluida, MaintenanceOrderStatus.Cancelada },
        [MaintenanceOrderStatus.Concluida] = Array.Empty<MaintenanceOrderStatus>(),
        [MaintenanceOrderStatus.Cancelada] = Array.Empty<MaintenanceOrderStatus>()
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ManutencoesController> _logger;
    private readonly IAuditTrailService _auditTrailService;

    public ManutencoesController(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        ILogger<ManutencoesController> logger,
        IAuditTrailService auditTrailService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
        _auditTrailService = auditTrailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] MaintenanceOrderStatus? status,
        [FromQuery] Guid? imovelId,
        [FromQuery] DateTime? criadaDe,
        [FromQuery] DateTime? criadaAte,
        CancellationToken cancellationToken)
    {
        var query = _context.Manutencoes
            .AsNoTracking()
            .Include(m => m.Imovel)
            .AsQueryable();

        if (status.HasValue)
        {
            var filtro = status.Value;
            query = query.Where(m => m.Status == filtro);
        }

        if (imovelId.HasValue)
        {
            var filtro = imovelId.Value;
            query = query.Where(m => m.ImovelId == filtro);
        }

        if (criadaDe.HasValue)
        {
            var inicio = NormalizeDate(criadaDe.Value);
            query = query.Where(m => m.CreatedAt >= inicio);
        }

        if (criadaAte.HasValue)
        {
            var fim = NormalizeDate(criadaAte.Value).AddDays(1).AddTicks(-1);
            query = query.Where(m => m.CreatedAt <= fim);
        }

        var ordens = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var agora = DateTime.UtcNow;
        var itens = ordens
            .Select(m => new MaintenanceOrderListItemViewModel
            {
                Id = m.Id,
                ImovelCodigo = m.Imovel?.CodigoInterno ?? string.Empty,
                ImovelTitulo = m.Imovel?.Titulo ?? string.Empty,
                Titulo = m.Titulo,
                Categoria = m.Categoria,
                Status = m.Status,
                CustoEstimado = m.CustoEstimado,
                CustoReal = m.CustoReal,
                CriadoEm = m.CreatedAt,
                PrevisaoConclusao = m.PrevisaoConclusao,
                DataConclusao = m.DataConclusao,
                EmExecucao = m.Status == MaintenanceOrderStatus.EmExecucao,
                EstaAtrasada = !FinalStatuses.Contains(m.Status) && m.PrevisaoConclusao.HasValue && m.PrevisaoConclusao.Value < agora
            })
            .ToList();

        var totais = Enum.GetValues<MaintenanceOrderStatus>()
            .ToDictionary(s => s, _ => 0);

        foreach (var item in itens)
        {
            totais[item.Status]++;
        }

        var imoveis = await _context.Imoveis
            .AsNoTracking()
            .OrderBy(i => i.CodigoInterno)
            .Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = $"{i.CodigoInterno} - {i.Titulo}",
                Selected = imovelId.HasValue && imovelId.Value == i.Id
            })
            .ToListAsync(cancellationToken);

        var model = new MaintenanceOrderListViewModel
        {
            Status = status,
            ImovelId = imovelId,
            CriadaDe = criadaDe,
            CriadaAte = criadaAte,
            Imoveis = imoveis,
            Itens = itens,
            TotaisPorStatus = totais,
            Total = itens.Count,
            TotalEmExecucao = itens.Count(i => i.EmExecucao),
            TotalAtrasadas = itens.Count(i => i.EstaAtrasada)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create([FromQuery] Guid? imovelId, CancellationToken cancellationToken)
    {
        var model = await BuildCreateViewModelAsync(new MaintenanceOrderCreateInputModel
        {
            ImovelId = imovelId
        }, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceOrderCreateInputModel ordem, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildCreateViewModelAsync(ordem, cancellationToken);
            return View(invalidModel);
        }

        var property = await _context.Imoveis
            .Include(i => i.Manutencoes)
            .FirstOrDefaultAsync(i => i.Id == ordem.ImovelId, cancellationToken);

        if (property is null)
        {
            ModelState.AddModelError(nameof(MaintenanceOrderCreateInputModel.ImovelId), "Imóvel não encontrado.");
            var invalidModel = await BuildCreateViewModelAsync(ordem, cancellationToken);
            return View(invalidModel);
        }

        if (ordem.VistoriaId.HasValue)
        {
            var vistoria = await _context.Vistorias
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == ordem.VistoriaId && v.ImovelId == property.Id, cancellationToken);

            if (vistoria is null)
            {
                ModelState.AddModelError(nameof(MaintenanceOrderCreateInputModel.VistoriaId), "Vistoria selecionada é inválida para o imóvel.");
                var invalidModel = await BuildCreateViewModelAsync(ordem, cancellationToken);
                return View(invalidModel);
            }
        }

        var usuario = User?.Identity?.Name ?? "Sistema";

        var novaOrdem = new MaintenanceOrder
        {
            ImovelId = property.Id,
            Titulo = ordem.Titulo!.Trim(),
            Descricao = ordem.Descricao!.Trim(),
            Categoria = ordem.Categoria?.Trim() ?? string.Empty,
            Responsavel = ordem.Responsavel?.Trim() ?? string.Empty,
            Contato = ordem.Contato?.Trim() ?? string.Empty,
            CustoEstimado = ordem.CustoEstimado,
            PrevisaoConclusao = ordem.PrevisaoConclusao.HasValue ? NormalizeDate(ordem.PrevisaoConclusao.Value) : null,
            VistoriaId = ordem.VistoriaId,
            Status = MaintenanceOrderStatus.Solicitada,
            StatusDisponibilidadeAnterior = property.StatusDisponibilidade,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = usuario
        };

        _context.Manutencoes.Add(novaOrdem);

        if (property.StatusDisponibilidade != AvailabilityStatus.EmManutencao)
        {
            property.StatusDisponibilidade = AvailabilityStatus.EmManutencao;
            await RegistrarEventoHistoricoAsync(property.Id, "Imóvel em manutenção", $"Status alterado para EmManutenção pela ordem {novaOrdem.Titulo}. OrdemManutencao:{novaOrdem.Id}", usuario, cancellationToken);
        }

        await RegistrarEventoHistoricoAsync(property.Id, "Ordem de manutenção criada", $"Ordem {novaOrdem.Titulo} registrada. OrdemManutencao:{novaOrdem.Id}", usuario, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaOrdemAsync(
            novaOrdem,
            "CREATE",
            string.Empty,
            SerializeOrdem(novaOrdem),
            cancellationToken);

        _logger.LogInformation("Ordem de manutenção {OrdemId} criada para o imóvel {ImovelId}", novaOrdem.Id, property.Id);

        return RedirectToAction(nameof(Details), new { id = novaOrdem.Id });
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
    public async Task<IActionResult> Update(Guid id, [Bind(Prefix = nameof(MaintenanceOrderDetailViewModel.Atualizacao))] MaintenanceOrderUpdateInputModel input, CancellationToken cancellationToken)
    {
        var ordem = await _context.Manutencoes
            .Include(m => m.Imovel)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (ordem is null)
        {
            return NotFound();
        }

        var antes = SerializeOrdem(ordem);
        if (!ModelState.IsValid)
        {
            var invalid = await LoadDetailModelAsync(id, cancellationToken, input);
            return View("Details", invalid);
        }

        var statusAtual = ordem.Status;
        var novoStatus = input.Status;

        if (statusAtual != novoStatus)
        {
            if (!AllowedTransitions.TryGetValue(statusAtual, out var permitidos) || !permitidos.Contains(novoStatus))
            {
                ModelState.AddModelError(nameof(MaintenanceOrderUpdateInputModel.Status), "Transição de {statusAtual} para {novoStatus} não é permitida.");
                var invalid = await LoadDetailModelAsync(id, cancellationToken, input);
                return View("Details", invalid);
            }
        }

        var agora = DateTime.UtcNow;
        var usuario = User?.Identity?.Name ?? "Sistema";
        var alteracoes = new List<string>();

        if (!string.Equals(ordem.Categoria, input.Categoria?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            alteracoes.Add("Categoria: {ordem.Categoria} → {input.Categoria}");
            ordem.Categoria = input.Categoria?.Trim() ?? string.Empty;
        }

        if (!string.Equals(ordem.Responsavel, input.Responsavel?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            alteracoes.Add("Responsável: {ordem.Responsavel} → {input.Responsavel}");
            ordem.Responsavel = input.Responsavel?.Trim() ?? string.Empty;
        }

        if (!string.Equals(ordem.Contato, input.Contato?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            alteracoes.Add("Contato: {ordem.Contato} → {input.Contato}");
            ordem.Contato = input.Contato?.Trim() ?? string.Empty;
        }

        if (ordem.CustoEstimado != input.CustoEstimado)
        {
            alteracoes.Add("Custo estimado: {ordem.CustoEstimado:C} → {input.CustoEstimado:C}");
            ordem.CustoEstimado = input.CustoEstimado;
        }

        if (ordem.CustoReal != input.CustoReal)
        {
            alteracoes.Add("Custo real: {ordem.CustoReal:C} → {input.CustoReal:C}");
            ordem.CustoReal = input.CustoReal;
        }

        var previsao = input.PrevisaoConclusao.HasValue ? NormalizeDate(input.PrevisaoConclusao.Value) : null;
        if (ordem.PrevisaoConclusao != previsao)
        {
            alteracoes.Add("Previsão conclusão: {FormatDate(ordem.PrevisaoConclusao)} → {FormatDate(previsao)}");
            ordem.PrevisaoConclusao = previsao;
        }

        if (statusAtual != novoStatus)
        {
            alteracoes.Add("Status: {statusAtual} → {novoStatus}");
            ordem.Status = novoStatus;

            if (novoStatus == MaintenanceOrderStatus.EmExecucao)
            {
                ordem.IniciadaEm = agora;
            }

            if (FinalStatuses.Contains(novoStatus))
            {
                ordem.DataConclusao = agora;
            }
        }

        ordem.UpdatedAt = agora;
        ordem.UpdatedBy = usuario;

        if (alteracoes.Count == 0 && string.IsNullOrWhiteSpace(input.Observacoes))
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var comentario = string.Join(" | ", alteracoes);
        if (!string.IsNullOrWhiteSpace(input.Observacoes))
        {
            comentario = string.IsNullOrWhiteSpace(comentario)
                ? input.Observacoes!.Trim()
                : $"{comentario} | {input.Observacoes!.Trim()}";
        }

        await RegistrarEventoHistoricoAsync(ordem.ImovelId, "Atualização ordem de manutenção", $"{comentario}. OrdemManutencao:{ordem.Id}", usuario, cancellationToken);

        if (FinalStatuses.Contains(ordem.Status))
        {
            await NormalizarStatusImovelAsync(ordem, cancellationToken);
        }
        else
        {
            await GarantirImovelEmManutencaoAsync(ordem, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaOrdemAsync(
            ordem,
            "UPDATE",
            antes,
            SerializeOrdem(ordem),
            cancellationToken);

        _logger.LogInformation("Ordem de manutenção {OrdemId} atualizada por {Usuario}", ordem.Id, usuario);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocumento(Guid id, [Bind(Prefix = nameof(MaintenanceOrderDetailViewModel.NovoDocumento))] MaintenanceOrderAttachmentInputModel input, CancellationToken cancellationToken)
    {
        var ordem = await _context.Manutencoes
            .Include(m => m.Documentos)
                .ThenInclude(d => d.Arquivo)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (ordem is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid || input.Arquivo is null)
        {
            var invalid = await LoadDetailModelAsync(id, cancellationToken);
            return View("Details", invalid);
        }

        await using var stream = input.Arquivo.OpenReadStream();
        var stored = await _fileStorageService.SaveAsync(
            input.Arquivo.FileName,
            input.Arquivo.ContentType,
            stream,
            "manutencoes",
            cancellationToken);

        stored.CreatedBy = User?.Identity?.Name ?? "Sistema";

        var documento = new MaintenanceOrderDocument
        {
            OrdemManutencaoId = ordem.Id,
            ArquivoId = stored.Id,
            Arquivo = stored,
            Categoria = input.Categoria?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = stored.CreatedBy
        };

        _context.Arquivos.Add(stored);
        _context.MaintenanceDocuments.Add(documento);

        await RegistrarEventoHistoricoAsync(ordem.ImovelId, "Documento anexado", $"Arquivo {stored.NomeOriginal} incluído. OrdemManutencao:{ordem.Id}", stored.CreatedBy, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaDocumentoAsync(
            documento,
            "CREATE",
            string.Empty,
            SerializeDocumento(documento),
            cancellationToken);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverDocumento(Guid id, Guid documentoId, CancellationToken cancellationToken)
    {
        var documento = await _context.MaintenanceDocuments
            .Include(d => d.Arquivo)
            .Include(d => d.OrdemManutencao)
            .FirstOrDefaultAsync(d => d.Id == documentoId && d.OrdemManutencaoId == id, cancellationToken);

        if (documento is null)
        {
            return NotFound();
        }

        var documentoAntes = SerializeDocumento(documento);

        _context.MaintenanceDocuments.Remove(documento);

        if (documento.Arquivo is not null)
        {
            await _fileStorageService.DeleteAsync(documento.Arquivo, cancellationToken);
            _context.Arquivos.Remove(documento.Arquivo);
        }

        var usuario = User?.Identity?.Name ?? "Sistema";
        if (documento.OrdemManutencao is not null)
        {
            await RegistrarEventoHistoricoAsync(documento.OrdemManutencao.ImovelId, "Documento removido", $"Documento {documento.Arquivo?.NomeOriginal} removido. OrdemManutencao:{documento.OrdemManutencaoId}", usuario, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaDocumentoAsync(
            documento,
            "DELETE",
            documentoAntes,
            string.Empty,
            cancellationToken);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocumento(Guid id, Guid documentoId, CancellationToken cancellationToken)
    {
        var documento = await _context.MaintenanceDocuments
            .Include(d => d.Arquivo)
            .FirstOrDefaultAsync(d => d.Id == documentoId && d.OrdemManutencaoId == id, cancellationToken);

        if (documento?.Arquivo is null)
        {
            return NotFound();
        }

        var stream = await _fileStorageService.OpenAsync(documento.Arquivo, cancellationToken);

        await RegistrarAuditoriaDocumentoAsync(
            documento,
            "DOWNLOAD",
            string.Empty,
            string.Empty,
            cancellationToken);

        return File(stream, documento.Arquivo.ConteudoTipo, documento.Arquivo.NomeOriginal);
    }

    private async Task<MaintenanceOrderCreateViewModel> BuildCreateViewModelAsync(MaintenanceOrderCreateInputModel input, CancellationToken cancellationToken)
    {
        var imoveis = await _context.Imoveis
            .AsNoTracking()
            .OrderBy(i => i.CodigoInterno)
            .Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = $"{i.CodigoInterno} - {i.Titulo}",
                Selected = input.ImovelId.HasValue && input.ImovelId.Value == i.Id
            })
            .ToListAsync(cancellationToken);

        var vistorias = Array.Empty<SelectListItem>();

        if (input.ImovelId.HasValue)
        {
            vistorias = await _context.Vistorias
                .AsNoTracking()
                .Where(v => v.ImovelId == input.ImovelId.Value)
                .OrderByDescending(v => v.AgendadaPara)
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.Tipo} - {v.AgendadaPara:dd/MM/yyyy}",
                    Selected = input.VistoriaId.HasValue && input.VistoriaId.Value == v.Id
                })
                .ToListAsync(cancellationToken);
        }

        return new MaintenanceOrderCreateViewModel
        {
            Ordem = input,
            Imoveis = imoveis,
            Vistorias = vistorias
        };
    }

    private async Task<MaintenanceOrderDetailViewModel?> LoadDetailModelAsync(Guid id, CancellationToken cancellationToken, MaintenanceOrderUpdateInputModel? inputOverride = null)
    {
        var ordem = await _context.Manutencoes
            .Include(m => m.Imovel)
            .Include(m => m.Vistoria)
            .Include(m => m.Documentos)
                .ThenInclude(d => d.Arquivo)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (ordem is null)
        {
            return null;
        }

        var documentos = ordem.Documentos
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new MaintenanceOrderDocumentViewModel
            {
                Id = d.Id,
                ArquivoId = d.ArquivoId,
                Nome = d.Arquivo?.NomeOriginal ?? "Arquivo",
                Categoria = d.Categoria,
                ContentType = d.Arquivo?.ConteudoTipo ?? "application/octet-stream",
                Tamanho = d.Arquivo?.TamanhoEmBytes ?? 0,
                UploadEm = d.CreatedAt
            })
            .ToList();

        var timeline = await BuildTimelineAsync(ordem, documentos, cancellationToken);

        var atualizacao = inputOverride ?? new MaintenanceOrderUpdateInputModel
        {
            Status = ordem.Status,
            CustoEstimado = ordem.CustoEstimado,
            CustoReal = ordem.CustoReal,
            Categoria = ordem.Categoria,
            PrevisaoConclusao = ordem.PrevisaoConclusao?.ToLocalTime().Date,
            Responsavel = ordem.Responsavel,
            Contato = ordem.Contato
        };

        var model = new MaintenanceOrderDetailViewModel
        {
            Id = ordem.Id,
            Titulo = ordem.Titulo,
            Descricao = ordem.Descricao,
            Categoria = ordem.Categoria,
            Status = ordem.Status,
            CustoEstimado = ordem.CustoEstimado,
            CustoReal = ordem.CustoReal,
            CriadoEm = ordem.CreatedAt,
            IniciadaEm = ordem.IniciadaEm,
            PrevisaoConclusao = ordem.PrevisaoConclusao,
            DataConclusao = ordem.DataConclusao,
            Responsavel = ordem.Responsavel,
            Contato = ordem.Contato,
            ImovelId = ordem.ImovelId,
            ImovelCodigo = ordem.Imovel?.CodigoInterno ?? string.Empty,
            ImovelTitulo = ordem.Imovel?.Titulo ?? string.Empty,
            VistoriaId = ordem.VistoriaId,
            VistoriaDescricao = ordem.Vistoria is null ? null : $"{ordem.Vistoria.Tipo} - {ordem.Vistoria.AgendadaPara:dd/MM/yyyy}",
            Documentos = documentos,
            Timeline = timeline,
            Atualizacao = atualizacao,
            NovoDocumento = new MaintenanceOrderAttachmentInputModel()
        };

        return model;
    }

    private async Task<IReadOnlyCollection<MaintenanceOrderTimelineItemViewModel>> BuildTimelineAsync(MaintenanceOrder ordem, IReadOnlyCollection<MaintenanceOrderDocumentViewModel> documentos, CancellationToken cancellationToken)
    {
        var eventos = new List<MaintenanceOrderTimelineItemViewModel>
        {
            new()
            {
                Titulo = "Abertura da ordem",
                Descricao = ordem.Descricao,
                OcorreuEm = ordem.CreatedAt,
                Usuario = ordem.CreatedBy
            }
        };

        var marcador = "OrdemManutencao:{ordem.Id}";

        var historico = await _context.PropertyHistory
            .AsNoTracking()
            .Where(h => h.ImovelId == ordem.ImovelId && h.Descricao.Contains(marcador))
            .OrderByDescending(h => h.OcorreuEm)
            .ToListAsync(cancellationToken);

        foreach (var item in historico)
        {
            eventos.Add(new MaintenanceOrderTimelineItemViewModel
            {
                Titulo = item.Titulo,
                Descricao = item.Descricao.Replace(marcador, string.Empty, StringComparison.OrdinalIgnoreCase).Trim(),
                OcorreuEm = item.OcorreuEm,
                Usuario = item.Usuario
            });
        }

        foreach (var doc in documentos)
        {
            eventos.Add(new MaintenanceOrderTimelineItemViewModel
            {
                Titulo = "Documento anexado",
                Descricao = $"{doc.Nome} ({doc.Categoria})",
                OcorreuEm = doc.UploadEm,
                Usuario = ordem.UpdatedBy ?? ordem.CreatedBy
            });
        }

        return eventos
            .OrderByDescending(e => e.OcorreuEm)
            .ToList();
    }

    private async Task RegistrarEventoHistoricoAsync(Guid imovelId, string titulo, string descricao, string usuario, CancellationToken cancellationToken)
    {
        var evento = new PropertyHistoryEvent
        {
            ImovelId = imovelId,
            Titulo = titulo,
            Descricao = descricao,
            Usuario = usuario,
            OcorreuEm = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = usuario
        };

        await _context.PropertyHistory.AddAsync(evento, cancellationToken);
    }

    private async Task GarantirImovelEmManutencaoAsync(MaintenanceOrder ordem, CancellationToken cancellationToken)
    {
        var imovel = await _context.Imoveis.FirstOrDefaultAsync(i => i.Id == ordem.ImovelId, cancellationToken);
        if (imovel is null)
        {
            return;
        }

        if (imovel.StatusDisponibilidade != AvailabilityStatus.EmManutencao)
        {
            imovel.StatusDisponibilidade = AvailabilityStatus.EmManutencao;
            var usuario = User?.Identity?.Name ?? "Sistema";
            await RegistrarEventoHistoricoAsync(imovel.Id, "Imóvel em manutenção", $"Status ajustado para EmManutenção pela ordem {ordem.Titulo}. OrdemManutencao:{ordem.Id}", usuario, cancellationToken);
        }
    }

    private async Task NormalizarStatusImovelAsync(MaintenanceOrder ordem, CancellationToken cancellationToken)
    {
        var imovel = await _context.Imoveis
            .Include(i => i.Manutencoes)
            .FirstOrDefaultAsync(i => i.Id == ordem.ImovelId, cancellationToken);

        if (imovel is null)
        {
            return;
        }

        var possuiAtivas = imovel.Manutencoes.Any(m => m.Id != ordem.Id && !FinalStatuses.Contains(m.Status));
        if (!possuiAtivas)
        {
            var usuario = User?.Identity?.Name ?? "Sistema";
            var novoStatus = ordem.StatusDisponibilidadeAnterior ?? AvailabilityStatus.Disponivel;
            if (imovel.StatusDisponibilidade != novoStatus)
            {
                imovel.StatusDisponibilidade = novoStatus;
                await RegistrarEventoHistoricoAsync(imovel.Id, "Imóvel liberado", $"Status restaurado para {novoStatus} após conclusão da ordem {ordem.Titulo}. OrdemManutencao:{ordem.Id}", usuario, cancellationToken);
            }
        }
    }

    private static DateTime NormalizeDate(DateTime data)
    {
        return DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
    }

    private static string FormatDate(DateTime? data)
    {
        return data.HasValue ? data.Value.ToString("dd/MM/yyyy") : "-";
    }

    private async Task RegistrarAuditoriaOrdemAsync(
        MaintenanceOrder ordem,
        string operacao,
        string antes,
        string depois,
        CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync(
            "MaintenanceOrder",
            ordem.Id,
            operacao,
            antes,
            depois,
            usuario,
            ip,
            host,
            cancellationToken);
    }

    private async Task RegistrarAuditoriaDocumentoAsync(
        MaintenanceOrderDocument documento,
        string operacao,
        string antes,
        string depois,
        CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync(
            "MaintenanceOrderDocument",
            documento.Id,
            operacao,
            antes,
            depois,
            usuario,
            ip,
            host,
            cancellationToken);
    }

    private static string SerializeOrdem(MaintenanceOrder ordem)
    {
        var payload = new
        {
            ordem.Id,
            ordem.ImovelId,
            ordem.Titulo,
            ordem.Categoria,
            ordem.Status,
            ordem.PrevisaoConclusao,
            ordem.IniciadaEm,
            ordem.CustoEstimado,
            ordem.CustoReal,
            ordem.DataConclusao,
            ordem.Responsavel,
            ordem.Contato,
            ordem.StatusDisponibilidadeAnterior,
            ordem.VistoriaId,
            ordem.CreatedAt,
            ordem.CreatedBy,
            ordem.UpdatedAt,
            ordem.UpdatedBy
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeDocumento(MaintenanceOrderDocument documento)
    {
        var payload = new
        {
            documento.Id,
            documento.OrdemManutencaoId,
            documento.ArquivoId,
            documento.Categoria,
            documento.CreatedAt,
            documento.CreatedBy
        };

        return JsonSerializer.Serialize(payload);
    }
}
