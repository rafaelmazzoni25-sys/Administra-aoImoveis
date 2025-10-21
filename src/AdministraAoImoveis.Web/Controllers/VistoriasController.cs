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

[Authorize(Roles = RoleNames.VistoriaEquipe)]
public class VistoriasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrailService;
    private readonly IFileStorageService _fileStorageService;

    private const string StorageCategory = "inspection-documents";

    public VistoriasController(ApplicationDbContext context, IAuditTrailService auditTrailService, IFileStorageService fileStorageService)
    {
        _context = context;
        _auditTrailService = auditTrailService;
        _fileStorageService = fileStorageService;
    }

    public async Task<IActionResult> Index([FromQuery] InspectionStatus? status, [FromQuery] InspectionType? tipo, CancellationToken cancellationToken)
    {
        var query = _context.Vistorias.Include(v => v.Imovel).AsQueryable();

        if (status.HasValue)
        {
            var filtro = status.Value;
            query = query.Where(v => v.Status == filtro);
        }

        if (tipo.HasValue)
        {
            var filtro = tipo.Value;
            query = query.Where(v => v.Tipo == filtro);
        }

        var vistorias = await query
            .OrderBy(v => v.AgendadaPara)
            .ThenBy(v => v.Status)
            .ToListAsync(cancellationToken);

        var model = new InspectionListViewModel
        {
            Status = status,
            Tipo = tipo,
            Vistorias = vistorias
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = await MontarFormularioAsync(new InspectionFormViewModel
        {
            AgendadaPara = DateTime.UtcNow.AddDays(1)
        }, cancellationToken);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InspectionFormViewModel model, CancellationToken cancellationToken)
    {
        model = await MontarFormularioAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var imovel = await _context.Imoveis
            .Include(p => p.Vistorias)
            .FirstOrDefaultAsync(p => p.Id == model.ImovelId, cancellationToken);

        if (imovel is null)
        {
            ModelState.AddModelError(nameof(model.ImovelId), "Imóvel não encontrado.");
            return View("Form", model);
        }

        var conflito = await _context.Vistorias.AnyAsync(v => v.ImovelId == model.ImovelId
            && v.AgendadaPara == model.AgendadaPara
            && v.Id != model.Id, cancellationToken);

        if (conflito)
        {
            ModelState.AddModelError(string.Empty, "Já existe uma vistoria agendada para este imóvel neste horário.");
            return View("Form", model);
        }

        var vistoria = new Inspection
        {
            ImovelId = model.ImovelId,
            Tipo = model.Tipo,
            AgendadaPara = model.AgendadaPara,
            Responsavel = model.Responsavel,
            ChecklistJson = model.ChecklistJson,
            Observacoes = model.Observacoes,
            Status = InspectionStatus.Agendada
        };

        _context.Vistorias.Add(vistoria);
        await _context.SaveChangesAsync(cancellationToken);

        await CriarEntradaAgendaAsync(vistoria, cancellationToken);
        await AtualizarDisponibilidadeImovelAsync(imovel, vistoria.Tipo, cancellationToken);
        await RegistrarAuditoriaAsync(vistoria, "CREATE", string.Empty, JsonSerializer.Serialize(vistoria), cancellationToken);

        TempData["Success"] = "Vistoria agendada com sucesso.";
        return RedirectToAction(nameof(Details), new { id = vistoria.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var vistoria = await _context.Vistorias.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vistoria is null)
        {
            return NotFound();
        }

        var model = await MontarFormularioAsync(new InspectionFormViewModel
        {
            Id = vistoria.Id,
            ImovelId = vistoria.ImovelId,
            Tipo = vistoria.Tipo,
            AgendadaPara = vistoria.AgendadaPara,
            Responsavel = vistoria.Responsavel,
            ChecklistJson = vistoria.ChecklistJson,
            Observacoes = vistoria.Observacoes,
            Status = vistoria.Status,
            Inicio = vistoria.Inicio,
            Fim = vistoria.Fim,
            PodeEditar = vistoria.Status != InspectionStatus.Concluida
        }, cancellationToken);

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, InspectionFormViewModel model, CancellationToken cancellationToken)
    {
        var vistoria = await _context.Vistorias
            .Include(v => v.Imovel)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vistoria is null)
        {
            return NotFound();
        }

        if (vistoria.Status == InspectionStatus.Concluida)
        {
            TempData["Error"] = "Vistorias concluídas não podem ser editadas.";
            return RedirectToAction(nameof(Details), new { id });
        }

        model.Id = id;
        model = await MontarFormularioAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var conflito = await _context.Vistorias.AnyAsync(v => v.ImovelId == model.ImovelId
            && v.AgendadaPara == model.AgendadaPara
            && v.Id != vistoria.Id, cancellationToken);

        if (conflito)
        {
            ModelState.AddModelError(string.Empty, "Já existe uma vistoria agendada para este imóvel neste horário.");
            return View("Form", model);
        }

        var antes = JsonSerializer.Serialize(vistoria);

        vistoria.ImovelId = model.ImovelId;
        vistoria.Tipo = model.Tipo;
        vistoria.AgendadaPara = model.AgendadaPara;
        vistoria.Responsavel = model.Responsavel;
        vistoria.ChecklistJson = model.ChecklistJson;
        vistoria.Observacoes = model.Observacoes;
        vistoria.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await AtualizarDisponibilidadeImovelAsync(vistoria.Imovel!, vistoria.Tipo, cancellationToken);
        await RegistrarAuditoriaAsync(vistoria, "UPDATE", antes, JsonSerializer.Serialize(vistoria), cancellationToken);

        TempData["Success"] = "Vistoria atualizada.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var vistoria = await _context.Vistorias
            .Include(v => v.Imovel)
            .Include(v => v.Documentos)
                .ThenInclude(d => d.Arquivo)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vistoria is null)
        {
            return NotFound();
        }

        var model = new InspectionFormViewModel
        {
            Id = vistoria.Id,
            ImovelId = vistoria.ImovelId,
            Tipo = vistoria.Tipo,
            AgendadaPara = vistoria.AgendadaPara,
            Responsavel = vistoria.Responsavel,
            ChecklistJson = vistoria.ChecklistJson,
            Observacoes = vistoria.Observacoes,
            Status = vistoria.Status,
            Inicio = vistoria.Inicio,
            Fim = vistoria.Fim,
            PodeEditar = vistoria.Status != InspectionStatus.Concluida
        };

        model = await MontarFormularioAsync(model, cancellationToken);
        model.Documentos = vistoria.Documentos
            .OrderByDescending(d => d.CreatedAt)
            .Select(MapDocument)
            .ToList();
        model.Upload = new InspectionDocumentUploadInputModel();
        return View("Details", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAndamento(Guid id, InspectionStatus status, string? checklistJson, string? observacoes, string? pendenciasTexto, CancellationToken cancellationToken)
    {
        var vistoria = await _context.Vistorias
            .Include(v => v.Imovel)
            .Include(v => v.Imovel!.Atividades)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vistoria is null)
        {
            return NotFound();
        }

        var antes = JsonSerializer.Serialize(vistoria);

        vistoria.Status = status;
        vistoria.ChecklistJson = checklistJson ?? "{}";
        vistoria.Observacoes = observacoes ?? string.Empty;

        if (status == InspectionStatus.EmAndamento)
        {
            vistoria.Inicio ??= DateTime.UtcNow;
        }

        if (status == InspectionStatus.Concluida)
        {
            vistoria.Fim = DateTime.UtcNow;
            await GerarPendenciasAsync(vistoria, pendenciasTexto, cancellationToken);
            await AvaliarDisponibilidadePosConclusaoAsync(vistoria, cancellationToken);
        }

        if (status == InspectionStatus.RelatorioPendente)
        {
            vistoria.Fim = DateTime.UtcNow;
        }

        vistoria.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(vistoria, "STATUS_UPDATE", antes, JsonSerializer.Serialize(vistoria), cancellationToken);

        TempData["Success"] = "Status da vistoria atualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocumento(Guid id, InspectionDocumentUploadInputModel input, CancellationToken cancellationToken)
    {
        var vistoria = await _context.Vistorias
            .Include(v => v.Documentos)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vistoria is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid || input.Arquivo is null)
        {
            var mensagem = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "Informe o tipo e selecione um arquivo.";

            TempData["Error"] = mensagem;
            return RedirectToAction(nameof(Details), new { id });
        }

        var usuario = User?.Identity?.Name ?? "Sistema";
        await using var stream = input.Arquivo.OpenReadStream();
        var stored = await _fileStorageService.SaveAsync(
            input.Arquivo.FileName,
            input.Arquivo.ContentType,
            stream,
            StorageCategory,
            cancellationToken);

        stored.CreatedBy = usuario;
        _context.Arquivos.Add(stored);

        var documento = new InspectionDocument
        {
            VistoriaId = vistoria.Id,
            ArquivoId = stored.Id,
            Arquivo = stored,
            Tipo = input.Tipo.Trim(),
            CreatedBy = usuario
        };

        vistoria.Documentos.Add(documento);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Documento anexado à vistoria.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocumento(Guid id, Guid documentoId, CancellationToken cancellationToken)
    {
        var documento = await _context.InspectionDocuments
            .Include(d => d.Arquivo)
            .FirstOrDefaultAsync(d => d.Id == documentoId && d.VistoriaId == id, cancellationToken);

        if (documento is null || documento.Arquivo is null)
        {
            return NotFound();
        }

        var stream = await _fileStorageService.OpenAsync(documento.Arquivo, cancellationToken);
        return File(stream, documento.Arquivo.ConteudoTipo, documento.Arquivo.NomeOriginal);
    }

    private async Task<InspectionFormViewModel> MontarFormularioAsync(InspectionFormViewModel model, CancellationToken cancellationToken)
    {
        var imoveis = await _context.Imoveis
            .OrderBy(i => i.CodigoInterno)
            .Select(i => new { i.Id, i.CodigoInterno })
            .ToListAsync(cancellationToken);

        model.Imoveis = imoveis.Select(i => (i.Id, i.CodigoInterno)).ToArray();
        return model;
    }

    private async Task CriarEntradaAgendaAsync(Inspection vistoria, CancellationToken cancellationToken)
    {
        var entrada = new ScheduleEntry
        {
            Titulo = $"Vistoria {vistoria.Tipo}",
            Tipo = "Vistoria",
            Setor = "Vistoria",
            Inicio = vistoria.AgendadaPara,
            Fim = vistoria.AgendadaPara.AddHours(1),
            Responsavel = vistoria.Responsavel,
            ImovelId = vistoria.ImovelId,
            VistoriaId = vistoria.Id,
            Observacoes = vistoria.Observacoes
        };

        _context.Agenda.Add(entrada);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task AtualizarDisponibilidadeImovelAsync(Property imovel, InspectionType tipo, CancellationToken cancellationToken)
    {
        if (tipo == InspectionType.Saida)
        {
            imovel.StatusDisponibilidade = AvailabilityStatus.EmVistoriaSaida;
        }
        else if (tipo == InspectionType.Entrada)
        {
            imovel.StatusDisponibilidade = AvailabilityStatus.EmVistoriaEntrada;
        }

        imovel.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task GerarPendenciasAsync(Inspection vistoria, string? pendenciasTexto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pendenciasTexto))
        {
            return;
        }

        var linhas = pendenciasTexto
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var linha in linhas)
        {
            var atividade = new Activity
            {
                Tipo = ActivityType.Pendencia,
                Titulo = linha,
                Descricao = linha,
                VinculoTipo = ActivityLinkType.Vistoria,
                VinculoId = vistoria.Id,
                Setor = "Operações",
                Responsavel = vistoria.Responsavel,
                Prioridade = PriorityLevel.Critica,
                Status = ActivityStatus.Aberta
            };

            _context.Atividades.Add(atividade);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task AvaliarDisponibilidadePosConclusaoAsync(Inspection vistoria, CancellationToken cancellationToken)
    {
        var imovel = await _context.Imoveis
            .Include(p => p.Atividades)
            .FirstOrDefaultAsync(p => p.Id == vistoria.ImovelId, cancellationToken);

        if (imovel is null)
        {
            return;
        }

        var possuiPendenciaCritica = imovel.Atividades.Any(a => a.Prioridade == PriorityLevel.Critica && a.Status != ActivityStatus.Concluida && a.Status != ActivityStatus.Cancelada);
        if (possuiPendenciaCritica)
        {
            imovel.StatusDisponibilidade = AvailabilityStatus.EmManutencao;
        }
        else if (vistoria.Tipo is InspectionType.Saida or InspectionType.RetornoManutencao)
        {
            imovel.StatusDisponibilidade = AvailabilityStatus.Disponivel;
            imovel.DataPrevistaDisponibilidade = DateTime.UtcNow;
        }

        imovel.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RegistrarAuditoriaAsync(Inspection vistoria, string operacao, string antes, string depois, CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync("Inspection", vistoria.Id, operacao, antes, depois, usuario, ip, host, cancellationToken);
    }

    private static InspectionDocumentViewModel MapDocument(InspectionDocument documento)
    {
        return new InspectionDocumentViewModel
        {
            Id = documento.Id,
            Tipo = documento.Tipo,
            NomeArquivo = documento.Arquivo?.NomeOriginal ?? "Arquivo",
            CreatedAt = documento.CreatedAt,
            CreatedBy = documento.CreatedBy
        };
    }
}
