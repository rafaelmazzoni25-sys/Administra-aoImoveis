using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class AgendaController : Controller
{
    private readonly ApplicationDbContext _context;

    public AgendaController(ApplicationDbContext context)
    {
        _context = context;
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
