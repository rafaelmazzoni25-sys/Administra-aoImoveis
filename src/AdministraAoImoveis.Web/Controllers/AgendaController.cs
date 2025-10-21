using System.Linq;
using System.Text.Json;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using AdministraAoImoveis.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class AgendaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrailService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AgendaController(ApplicationDbContext context, IAuditTrailService auditTrailService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _auditTrailService = auditTrailService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] string? modo,
        [FromQuery] string? tipo,
        [FromQuery] string? responsavel,
        [FromQuery] string? setor,
        CancellationToken cancellationToken)
    {
        var viewMode = NormalizeViewMode(modo);
        var normalizedStart = NormalizeDate(inicio) ?? EnsureUtc(DateTime.UtcNow.Date);

        DateTime start;
        DateTime end;

        switch (viewMode)
        {
            case AgendaViewMode.Semana:
                start = EnsureUtc(normalizedStart.Date);
                end = start.AddDays(7);
                break;
            case AgendaViewMode.Mes:
                var baseDate = EnsureUtc(normalizedStart.Date);
                start = new DateTime(baseDate.Year, baseDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                end = start.AddMonths(1);
                break;
            default:
                start = normalizedStart;
                end = NormalizeDate(fim) ?? start.AddDays(7);
                if (end <= start)
                {
                    end = start.AddDays(7);
                }
                break;
        }

        var baseQuery = await _context.Agenda
            .Where(a => a.Inicio < end && a.Fim > start)
            .OrderBy(a => a.Inicio)
            .ToListAsync(cancellationToken);

        var tipoFiltro = NormalizeFilter(tipo);
        var responsavelFiltro = NormalizeFilter(responsavel);
        var setorFiltro = NormalizeFilter(setor);

        var disponiveisTipos = baseQuery
            .Select(a => a.Tipo)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var disponiveisResponsaveis = baseQuery
            .Select(a => a.Responsavel)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var disponiveisSetores = baseQuery
            .Select(a => a.Setor)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var filtrados = baseQuery.AsEnumerable();

        if (tipoFiltro is not null)
        {
            filtrados = filtrados.Where(a => string.Equals(a.Tipo, tipoFiltro, StringComparison.OrdinalIgnoreCase));
        }

        if (responsavelFiltro is not null)
        {
            filtrados = filtrados.Where(a => string.Equals(a.Responsavel, responsavelFiltro, StringComparison.OrdinalIgnoreCase));
        }

        if (setorFiltro is not null)
        {
            filtrados = filtrados.Where(a => string.Equals(a.Setor, setorFiltro, StringComparison.OrdinalIgnoreCase));
        }

        var compromissosModel = MapWithConflicts(filtrados.ToList());

        var model = new ScheduleCalendarViewModel
        {
            Inicio = start,
            Fim = end,
            ModoVisualizacao = viewMode,
            TipoSelecionado = tipoFiltro,
            ResponsavelSelecionado = responsavelFiltro,
            SetorSelecionado = setorFiltro,
            TiposDisponiveis = disponiveisTipos,
            ResponsaveisDisponiveis = disponiveisResponsaveis,
            SetoresDisponiveis = disponiveisSetores,
            Compromissos = compromissosModel
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var modelo = await MontarFormularioAsync(new ScheduleEntryFormViewModel
        {
            Inicio = DateTime.UtcNow.AddHours(1),
            Fim = DateTime.UtcNow.AddHours(2)
        }, cancellationToken);

        return View("Form", modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScheduleEntryFormViewModel model, CancellationToken cancellationToken)
    {
        model = await MontarFormularioAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        await ValidarConflitosAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var entrada = MapearParaEntidade(model);

        _context.Agenda.Add(entrada);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(entrada, "CREATE", string.Empty, JsonSerializer.Serialize(entrada), cancellationToken);
        await CriarNotificacaoAsync(entrada, cancellationToken);

        TempData["Success"] = "Compromisso criado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var entrada = await _context.Agenda
            .Include(e => e.Imovel)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entrada is null)
        {
            return NotFound();
        }

        var model = await MontarFormularioAsync(MapearParaModelo(entrada), cancellationToken);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ScheduleEntryFormViewModel model, CancellationToken cancellationToken)
    {
        var entrada = await _context.Agenda.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entrada is null)
        {
            return NotFound();
        }

        model.Id = id;
        model = await MontarFormularioAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        await ValidarConflitosAsync(model, cancellationToken, id);

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var antes = JsonSerializer.Serialize(entrada);

        entrada.Titulo = model.Titulo;
        entrada.Tipo = model.Tipo;
        entrada.Setor = model.Setor ?? string.Empty;
        entrada.Inicio = EnsureUtc(model.Inicio);
        entrada.Fim = EnsureUtc(model.Fim);
        entrada.Responsavel = model.Responsavel ?? string.Empty;
        entrada.ImovelId = model.ImovelId;
        entrada.NegociacaoId = model.NegociacaoId;
        entrada.VistoriaId = model.VistoriaId;
        entrada.Observacoes = model.Observacoes ?? string.Empty;
        entrada.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaAsync(entrada, "UPDATE", antes, JsonSerializer.Serialize(entrada), cancellationToken);
        await CriarNotificacaoAsync(entrada, cancellationToken);

        TempData["Success"] = "Compromisso atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entrada = await _context.Agenda.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entrada is null)
        {
            return NotFound();
        }

        _context.Agenda.Remove(entrada);
        await _context.SaveChangesAsync(cancellationToken);

        await RegistrarAuditoriaAsync(entrada, "DELETE", JsonSerializer.Serialize(entrada), string.Empty, cancellationToken);

        TempData["Success"] = "Compromisso removido.";
        return RedirectToAction(nameof(Index));
    }

    private static DateTime? NormalizeDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var input = value.Value;
        return input.Kind switch
        {
            DateTimeKind.Utc => input,
            DateTimeKind.Local => input.ToUniversalTime(),
            _ => DateTime.SpecifyKind(input, DateTimeKind.Utc)
        };
    }

    private static string NormalizeViewMode(string? modo)
    {
        if (string.IsNullOrWhiteSpace(modo))
        {
            return AgendaViewMode.Semana;
        }

        var normalized = modo.Trim().ToLowerInvariant();
        return AgendaViewMode.IsValid(normalized)
            ? normalized
            : AgendaViewMode.Semana;
    }

    private static string? NormalizeFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static IReadOnlyCollection<ScheduleEntryViewModel> MapWithConflicts(IReadOnlyCollection<ScheduleEntry> compromissos)
    {
        if (compromissos.Count == 0)
        {
            return Array.Empty<ScheduleEntryViewModel>();
        }

        var buffers = compromissos
            .Select(entry => new ConflictAccumulator(entry))
            .ToDictionary(buffer => buffer.Entry.Id);

        var orderedEntries = compromissos.OrderBy(e => e.Inicio).ThenBy(e => e.Id).ToArray();

        for (var i = 0; i < orderedEntries.Length; i++)
        {
            for (var j = i + 1; j < orderedEntries.Length; j++)
            {
                var first = orderedEntries[i];
                var second = orderedEntries[j];

                if (!Overlaps(first, second))
                {
                    continue;
                }

                var shareResponsavel = HasSameResponsavel(first, second);
                var shareProperty = HasSameProperty(first, second);

                if (!shareResponsavel && !shareProperty)
                {
                    continue;
                }

                if (shareResponsavel)
                {
                    buffers[first.Id].AddConflict(second, "Responsável");
                    buffers[second.Id].AddConflict(first, "Responsável");
                }

                if (shareProperty)
                {
                    buffers[first.Id].AddConflict(second, "Imóvel");
                    buffers[second.Id].AddConflict(first, "Imóvel");
                }
            }
        }

        return orderedEntries
            .Select(entry => ScheduleEntryViewModel.FromEntity(entry, buffers[entry.Id].Build()))
            .ToArray();
    }

    private async Task<ScheduleEntryFormViewModel> MontarFormularioAsync(ScheduleEntryFormViewModel model, CancellationToken cancellationToken)
    {
        var imoveis = await _context.Imoveis
            .OrderBy(i => i.CodigoInterno)
            .Select(i => new { i.Id, i.CodigoInterno })
            .ToListAsync(cancellationToken);

        var negociacoes = await _context.Negociacoes
            .Where(n => n.Ativa)
            .Include(n => n.Imovel)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new { n.Id, ImovelCodigo = n.Imovel!.CodigoInterno })
            .ToListAsync(cancellationToken);

        var vistorias = await _context.Vistorias
            .Where(v => v.Status != AdministraAoImoveis.Web.Domain.Enumerations.InspectionStatus.Concluida)
            .OrderBy(v => v.AgendadaPara)
            .Select(v => new { v.Id, v.Tipo, v.AgendadaPara })
            .ToListAsync(cancellationToken);

        model.Imoveis = imoveis.Select(i => (Id: i.Id, Codigo: i.CodigoInterno)).ToArray();
        model.Negociacoes = negociacoes.Select(n => (Id: n.Id, Nome: n.ImovelCodigo)).ToArray();
        model.Vistorias = vistorias.Select(v => (Id: v.Id, Descricao: $"{v.Tipo} - {v.AgendadaPara:dd/MM HH:mm}")).ToArray();

        return model;
    }

    private ScheduleEntry MapearParaEntidade(ScheduleEntryFormViewModel model)
    {
        return new ScheduleEntry
        {
            Titulo = model.Titulo,
            Tipo = model.Tipo,
            Setor = model.Setor ?? string.Empty,
            Inicio = EnsureUtc(model.Inicio),
            Fim = EnsureUtc(model.Fim),
            Responsavel = model.Responsavel ?? string.Empty,
            ImovelId = model.ImovelId,
            VistoriaId = model.VistoriaId,
            NegociacaoId = model.NegociacaoId,
            Observacoes = model.Observacoes ?? string.Empty
        };
    }

    private ScheduleEntryFormViewModel MapearParaModelo(ScheduleEntry entrada)
    {
        return new ScheduleEntryFormViewModel
        {
            Id = entrada.Id,
            Titulo = entrada.Titulo,
            Tipo = entrada.Tipo,
            Setor = entrada.Setor,
            Inicio = entrada.Inicio,
            Fim = entrada.Fim,
            Responsavel = entrada.Responsavel,
            ImovelId = entrada.ImovelId,
            VistoriaId = entrada.VistoriaId,
            NegociacaoId = entrada.NegociacaoId,
            Observacoes = entrada.Observacoes
        };
    }

    private async Task ValidarConflitosAsync(ScheduleEntryFormViewModel model, CancellationToken cancellationToken, Guid? ignorarId = null)
    {
        if (model.Inicio >= model.Fim)
        {
            ModelState.AddModelError(nameof(model.Fim), "O horário final deve ser maior que o inicial.");
            return;
        }

        var query = _context.Agenda.AsQueryable();

        if (ignorarId.HasValue)
        {
            query = query.Where(e => e.Id != ignorarId.Value);
        }

        var sobrepostos = await query
            .Where(e => e.Inicio < model.Fim && e.Fim > model.Inicio)
            .ToListAsync(cancellationToken);

        foreach (var entrada in sobrepostos)
        {
            if (!string.IsNullOrWhiteSpace(model.Responsavel) && string.Equals(model.Responsavel, entrada.Responsavel, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Responsavel), "O responsável já possui um compromisso neste horário.");
            }

            if (model.ImovelId.HasValue && entrada.ImovelId == model.ImovelId)
            {
                ModelState.AddModelError(nameof(model.ImovelId), "O imóvel já possui compromisso neste horário.");
            }
        }
    }

    private async Task CriarNotificacaoAsync(ScheduleEntry entrada, CancellationToken cancellationToken)
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
            Titulo = "Novo compromisso na agenda",
            Mensagem = $"{entrada.Titulo} em {entrada.Inicio:dd/MM/yyyy HH:mm}",
            LinkDestino = Url.Action("Index", "Agenda"),
            Lida = false,
            CreatedBy = User?.Identity?.Name ?? "Sistema"
        };

        _context.Notificacoes.Add(notificacao);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RegistrarAuditoriaAsync(ScheduleEntry entrada, string operacao, string antes, string depois, CancellationToken cancellationToken)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync("ScheduleEntry", entrada.Id, operacao, antes, depois, usuario, ip, host, cancellationToken);
    }

    private static bool Overlaps(ScheduleEntry first, ScheduleEntry second)
    {
        return first.Inicio < second.Fim && second.Inicio < first.Fim;
    }

    private static bool HasSameResponsavel(ScheduleEntry first, ScheduleEntry second)
    {
        if (string.IsNullOrWhiteSpace(first.Responsavel) || string.IsNullOrWhiteSpace(second.Responsavel))
        {
            return false;
        }

        return string.Equals(first.Responsavel, second.Responsavel, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSameProperty(ScheduleEntry first, ScheduleEntry second)
    {
        if (!first.ImovelId.HasValue || !second.ImovelId.HasValue)
        {
            return false;
        }

        return first.ImovelId == second.ImovelId;
    }

    private sealed class ConflictAccumulator
    {
        public ConflictAccumulator(ScheduleEntry entry)
        {
            Entry = entry;
        }

        public ScheduleEntry Entry { get; }

        private readonly Dictionary<Guid, ConflictAccumulatorDetail> _conflicts = new();

        public void AddConflict(ScheduleEntry other, string escopo)
        {
            if (!_conflicts.TryGetValue(other.Id, out var conflict))
            {
                conflict = new ConflictAccumulatorDetail(other);
                conflict.AddScope(escopo);
                _conflicts.Add(other.Id, conflict);
                return;
            }

            conflict.AddScope(escopo);
        }

        public IReadOnlyCollection<ScheduleEntryConflictViewModel> Build()
        {
            return _conflicts.Values
                .OrderBy(c => c.Inicio)
                .ThenBy(c => c.Id)
                .Select(c => c.ToViewModel())
                .ToArray();
        }

        private sealed class ConflictAccumulatorDetail
        {
            private readonly HashSet<string> _escopos = new(StringComparer.OrdinalIgnoreCase);

            public ConflictAccumulatorDetail(ScheduleEntry entry)
            {
                Id = entry.Id;
                Titulo = entry.Titulo;
                Responsavel = entry.Responsavel;
                Inicio = entry.Inicio;
                Fim = entry.Fim;
            }

            public Guid Id { get; }
            public string Titulo { get; }
            public string Responsavel { get; }
            public DateTime Inicio { get; }
            public DateTime Fim { get; }

            public void AddScope(string escopo)
            {
                if (!string.IsNullOrWhiteSpace(escopo))
                {
                    _escopos.Add(escopo);
                }
            }

            public ScheduleEntryConflictViewModel ToViewModel()
            {
                var escopo = _escopos.Count == 0 ? string.Empty : string.Join(", ", _escopos.OrderBy(s => s));

                return new ScheduleEntryConflictViewModel
                {
                    Id = Id,
                    Titulo = Titulo,
                    Responsavel = Responsavel,
                    Inicio = Inicio,
                    Fim = Fim,
                    Escopo = escopo
                };
            }
        }
    }
}
