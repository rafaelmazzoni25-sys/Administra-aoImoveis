using System.Text.Json;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Models;
using AdministraAoImoveis.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class ImoveisController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrailService;

    public ImoveisController(ApplicationDbContext context, IAuditTrailService auditTrailService)
    {
        _context = context;
        _auditTrailService = auditTrailService;
    }

    public async Task<IActionResult> Index([FromQuery] AvailabilityStatus? status, [FromQuery] string? cidade, [FromQuery] string? bairro, [FromQuery] DateTime? disponivelAte, CancellationToken cancellationToken)
    {
        var query = _context.Imoveis.AsQueryable();

        if (status.HasValue)
        {
            var filtroStatus = status.Value;
            query = query.Where(p => p.StatusDisponibilidade == filtroStatus);
        }

        if (!string.IsNullOrWhiteSpace(cidade))
        {
            query = query.Where(p => p.Cidade == cidade);
        }

        if (!string.IsNullOrWhiteSpace(bairro))
        {
            query = query.Where(p => p.Bairro == bairro);
        }

        if (disponivelAte.HasValue)
        {
            var limite = disponivelAte.Value;
            query = query.Where(p => p.DataPrevistaDisponibilidade <= limite);
        }

        var results = await query
            .OrderBy(p => p.CodigoInterno)
            .Select(p => new PropertySummaryViewModel
            {
                Id = p.Id,
                CodigoInterno = p.CodigoInterno,
                Titulo = p.Titulo,
                Endereco = p.Endereco,
                Status = p.StatusDisponibilidade,
                DataPrevistaDisponibilidade = p.DataPrevistaDisponibilidade
            })
            .ToListAsync(cancellationToken);

        var model = new PropertyListViewModel
        {
            Status = status,
            Cidade = cidade,
            Bairro = bairro,
            DisponivelAte = disponivelAte,
            Resultados = results
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = await MontarFormularioAsync(new PropertyFormViewModel(), cancellationToken);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        model = await MontarFormularioAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var codigoExiste = await _context.Imoveis.AnyAsync(p => p.CodigoInterno == model.CodigoInterno, cancellationToken);
        if (codigoExiste)
        {
            ModelState.AddModelError(nameof(model.CodigoInterno), "Já existe um imóvel com este código interno.");
            return View("Form", model);
        }

        var property = new Property
        {
            CodigoInterno = model.CodigoInterno,
            Titulo = model.Titulo,
            Endereco = model.Endereco,
            Bairro = model.Bairro,
            Cidade = model.Cidade,
            Estado = model.Estado,
            Tipo = model.Tipo,
            Area = model.Area,
            Quartos = model.Quartos,
            Banheiros = model.Banheiros,
            Vagas = model.Vagas,
            CaracteristicasJson = model.CaracteristicasJson,
            ProprietarioId = model.ProprietarioId,
            StatusDisponibilidade = model.StatusDisponibilidade,
            DataPrevistaDisponibilidade = model.DataPrevistaDisponibilidade
        };

        _context.Imoveis.Add(property);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarHistoricoAsync(property, "Cadastro de imóvel", "Imóvel cadastrado no sistema.", cancellationToken);
        await RegistrarAuditoriaAsync(property, "CREATE", string.Empty, JsonSerializer.Serialize(property), cancellationToken);

        TempData["Success"] = "Imóvel cadastrado com sucesso.";
        return RedirectToAction(nameof(Details), new { id = property.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var property = await _context.Imoveis.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        var model = await MontarFormularioAsync(new PropertyFormViewModel
        {
            Id = property.Id,
            CodigoInterno = property.CodigoInterno,
            Titulo = property.Titulo,
            Endereco = property.Endereco,
            Bairro = property.Bairro,
            Cidade = property.Cidade,
            Estado = property.Estado,
            Tipo = property.Tipo,
            Area = property.Area,
            Quartos = property.Quartos,
            Banheiros = property.Banheiros,
            Vagas = property.Vagas,
            CaracteristicasJson = property.CaracteristicasJson,
            ProprietarioId = property.ProprietarioId,
            StatusDisponibilidade = property.StatusDisponibilidade,
            DataPrevistaDisponibilidade = property.DataPrevistaDisponibilidade
        }, cancellationToken);

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        var property = await _context.Imoveis.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (property is null)
        {
            return NotFound();
        }

        model.Id = id;
        model = await MontarFormularioAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var codigoExiste = await _context.Imoveis
            .AnyAsync(p => p.CodigoInterno == model.CodigoInterno && p.Id != id, cancellationToken);
        if (codigoExiste)
        {
            ModelState.AddModelError(nameof(model.CodigoInterno), "Já existe um imóvel com este código interno.");
            return View("Form", model);
        }

        var antes = JsonSerializer.Serialize(property);

        property.CodigoInterno = model.CodigoInterno;
        property.Titulo = model.Titulo;
        property.Endereco = model.Endereco;
        property.Bairro = model.Bairro;
        property.Cidade = model.Cidade;
        property.Estado = model.Estado;
        property.Tipo = model.Tipo;
        property.Area = model.Area;
        property.Quartos = model.Quartos;
        property.Banheiros = model.Banheiros;
        property.Vagas = model.Vagas;
        property.CaracteristicasJson = model.CaracteristicasJson;
        property.ProprietarioId = model.ProprietarioId;
        property.StatusDisponibilidade = model.StatusDisponibilidade;
        property.DataPrevistaDisponibilidade = model.DataPrevistaDisponibilidade;
        property.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarHistoricoAsync(property, "Atualização de cadastro", "Dados do imóvel atualizados.", cancellationToken);
        await RegistrarAuditoriaAsync(property, "UPDATE", antes, JsonSerializer.Serialize(property), cancellationToken);

        TempData["Success"] = "Imóvel atualizado com sucesso.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var property = await _context.Imoveis
            .Include(p => p.Proprietario)
            .Include(p => p.Negociacoes)
                .ThenInclude(n => n.Eventos)
            .Include(p => p.Negociacoes)
                .ThenInclude(n => n.Interessado)
            .Include(p => p.Vistorias)
            .Include(p => p.Atividades)
            .Include(p => p.Historico)
            .Include(p => p.Manutencoes)
            .Include(p => p.Documentos)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (property is null)
        {
            return NotFound();
        }

        await AtualizarDocumentosExpiradosAsync(property, cancellationToken);

        var agora = DateTime.UtcNow;
        var documentosResumo = property.Documentos
            .GroupBy(d => d.Descricao)
            .Select(grupo =>
            {
                var atual = grupo.OrderByDescending(d => d.Versao).First();
                var expirado = atual.ValidoAte.HasValue && atual.ValidoAte.Value < agora;
                var status = expirado && atual.Status == DocumentStatus.Aprovado
                    ? DocumentStatus.Expirado
                    : atual.Status;

                return new PropertyDocumentSummaryViewModel
                {
                    Descricao = atual.Descricao,
                    Status = status,
                    Versao = atual.Versao,
                    CreatedAt = atual.CreatedAt,
                    ValidoAte = atual.ValidoAte,
                    Expirado = expirado
                };
            })
            .OrderBy(d => d.Descricao)
            .ToList();

        var model = new PropertyDetailViewModel
        {
            Id = property.Id,
            CodigoInterno = property.CodigoInterno,
            Titulo = property.Titulo,
            Endereco = property.Endereco,
            Bairro = property.Bairro,
            Cidade = property.Cidade,
            Estado = property.Estado,
            Tipo = property.Tipo,
            Area = property.Area,
            Quartos = property.Quartos,
            Banheiros = property.Banheiros,
            Vagas = property.Vagas,
            StatusDisponibilidade = property.StatusDisponibilidade,
            DataPrevistaDisponibilidade = property.DataPrevistaDisponibilidade,
            CaracteristicasJson = property.CaracteristicasJson,
            Proprietario = property.Proprietario,
            Negociacoes = property.Negociacoes.ToList(),
            Vistorias = property.Vistorias.ToList(),
            Atividades = property.Atividades.ToList(),
            Historico = property.Historico
                .OrderByDescending(h => h.OcorreuEm)
                .ToList(),
            Documentos = documentosResumo,
            Manutencoes = property.Manutencoes
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new PropertyMaintenanceSummaryViewModel
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Status = m.Status,
                    CriadoEm = m.CreatedAt,
                    PrevisaoConclusao = m.PrevisaoConclusao,
                    EmExecucao = m.Status == MaintenanceOrderStatus.EmExecucao
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarDisponibilidade(Guid id, AvailabilityStatus status, DateTime? disponivelEm, CancellationToken cancellationToken)
    {
        var property = await _context.Imoveis
            .Include(p => p.Vistorias)
            .Include(p => p.Atividades)
            .Include(p => p.Negociacoes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (property is null)
        {
            return NotFound();
        }

        if (status == AvailabilityStatus.Disponivel)
        {
            var vistoriaPendente = property.Vistorias.Any(v => v.Status != InspectionStatus.Concluida);
            if (vistoriaPendente)
            {
                TempData["Error"] = "Não é possível liberar o imóvel enquanto houver vistoria pendente.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var pendenciaCritica = property.Atividades.Any(a => a.Prioridade == PriorityLevel.Critica && a.Status != ActivityStatus.Concluida && a.Status != ActivityStatus.Cancelada);
            if (pendenciaCritica)
            {
                TempData["Error"] = "Resolva as pendências críticas antes de liberar o imóvel.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        if (status == AvailabilityStatus.EmVistoriaSaida)
        {
            await GarantirVistoriaSaidaAgendada(property, cancellationToken);
        }

        if (status == AvailabilityStatus.Reservado || status == AvailabilityStatus.EmNegociacao)
        {
            var possuiAtiva = property.Negociacoes.Any(n => n.Ativa);
            if (!possuiAtiva)
            {
                TempData["Error"] = "Não existem negociações ativas para justificar esta mudança.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        var antes = JsonSerializer.Serialize(property);

        property.StatusDisponibilidade = status;
        property.DataPrevistaDisponibilidade = disponivelEm;
        property.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarHistoricoAsync(property, "Alteração de disponibilidade", $"Status atualizado para {status}.", cancellationToken);
        await RegistrarAuditoriaAsync(property, "AVAILABILITY_UPDATE", antes, JsonSerializer.Serialize(property), cancellationToken);

        TempData["Success"] = "Disponibilidade atualizada.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task AtualizarDocumentosExpiradosAsync(Property property, CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var usuario = User?.Identity?.Name ?? "Sistema";
        var alterado = false;

        foreach (var documento in property.Documentos)
        {
            if (documento.Status == DocumentStatus.Aprovado && documento.ValidoAte.HasValue && documento.ValidoAte.Value < agora)
            {
                documento.Status = DocumentStatus.Expirado;
                documento.UpdatedAt = agora;
                documento.UpdatedBy = usuario;
                alterado = true;
            }
        }

        if (alterado)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<PropertyFormViewModel> MontarFormularioAsync(PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        var proprietarios = await _context.Proprietarios
            .OrderBy(p => p.Nome)
            .Select(p => new { p.Id, p.Nome })
            .ToListAsync(cancellationToken);

        model.Proprietarios = proprietarios
            .Select(p => (p.Id, p.Nome))
            .ToArray();

        return model;
    }

    private async Task RegistrarHistoricoAsync(Property property, string titulo, string descricao, CancellationToken cancellationToken)
    {
        var evento = new PropertyHistoryEvent
        {
            ImovelId = property.Id,
            Titulo = titulo,
            Descricao = descricao,
            Usuario = User?.Identity?.Name ?? "Sistema",
            OcorreuEm = DateTime.UtcNow
        };

        _context.PropertyHistory.Add(evento);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RegistrarAuditoriaAsync(Property property, string operacao, string antes, string depois, CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync("Property", property.Id, operacao, antes, depois, usuario, ip, host, cancellationToken);
    }

    private async Task GarantirVistoriaSaidaAgendada(Property property, CancellationToken cancellationToken)
    {
        var existeSaida = property.Vistorias.Any(v => v.Tipo == InspectionType.Saida && v.Status != InspectionStatus.Concluida);
        if (existeSaida)
        {
            return;
        }

        var vistoria = new Inspection
        {
            ImovelId = property.Id,
            Tipo = InspectionType.Saida,
            Status = InspectionStatus.Agendada,
            AgendadaPara = DateTime.UtcNow.AddDays(1),
            Responsavel = User?.Identity?.Name ?? "Sistema",
            Observacoes = "Vistoria de saída gerada automaticamente a partir da alteração de disponibilidade."
        };

        _context.Vistorias.Add(vistoria);
        await _context.SaveChangesAsync(cancellationToken);

        var agenda = new ScheduleEntry
        {
            Titulo = $"Vistoria {vistoria.Tipo}",
            Tipo = "Vistoria",
            Setor = "Vistoria",
            Inicio = vistoria.AgendadaPara,
            Fim = vistoria.AgendadaPara.AddHours(1),
            Responsavel = vistoria.Responsavel,
            ImovelId = property.Id,
            VistoriaId = vistoria.Id,
            Observacoes = vistoria.Observacoes
        };

        _context.Agenda.Add(agenda);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
