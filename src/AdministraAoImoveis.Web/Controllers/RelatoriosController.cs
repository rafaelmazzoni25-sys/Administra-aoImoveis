using System.Globalization;
using System.Linq;
using System.Text;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize(Roles = RoleNames.Operacional)]
public class RelatoriosController : Controller
{
    private readonly ApplicationDbContext _context;
    private static readonly ActivityStatus[] FinalStatuses =
    {
        ActivityStatus.Concluida,
        ActivityStatus.Cancelada
    };

    public RelatoriosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Indicadores([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken cancellationToken)
    {
        var start = inicio ?? DateTime.UtcNow.AddMonths(-1);
        var end = fim ?? DateTime.UtcNow;

        var totalImoveis = await _context.Imoveis.CountAsync(cancellationToken);
        var disponiveis = await _context.Imoveis.CountAsync(p => p.StatusDisponibilidade == AvailabilityStatus.Disponivel, cancellationToken);
        var negociacoesPeriodo = await _context.Negociacoes
            .Include(n => n.Eventos)
            .Where(n => n.CreatedAt >= start && n.CreatedAt <= end)
            .ToListAsync(cancellationToken);
        var negociacoesPorEtapa = await _context.Negociacoes
            .Where(n => n.Ativa)
            .GroupBy(n => n.Etapa)
            .Select(g => new { Etapa = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);
        var vistorias = await _context.Vistorias
            .Where(v => v.Inicio != null && v.Fim != null && v.Inicio >= start && v.Fim <= end)
            .ToListAsync(cancellationToken);
        var manutencoes = await _context.Manutencoes
            .Where(m => m.DataConclusao != null && m.DataConclusao >= start && m.DataConclusao <= end && m.CustoReal.HasValue)
            .ToListAsync(cancellationToken);
        var manutencoesComDatas = await _context.Manutencoes
            .Where(m => m.DataConclusao != null && m.IniciadaEm != null && m.DataConclusao >= start && m.DataConclusao <= end)
            .ToListAsync(cancellationToken);
        var pendenciasCriticas = await _context.Atividades
            .CountAsync(a => a.Prioridade == PriorityLevel.Critica && !FinalStatuses.Contains(a.Status), cancellationToken);
        var pendenciasPorSetor = await _context.Atividades
            .Where(a => !FinalStatuses.Contains(a.Status))
            .GroupBy(a => string.IsNullOrWhiteSpace(a.Setor) ? "Não informado" : a.Setor)
            .Select(g => new { Setor = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);
        var financeiro = await _context.LancamentosFinanceiros
            .Where(l => l.CreatedAt >= start && l.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        var tempoMedioPorEtapa = Enum.GetValues<NegotiationStage>()
            .ToDictionary(
                etapa => etapa,
                etapa =>
                {
                    var naEtapa = negociacoesPeriodo.Where(n => n.Etapa == etapa).ToList();
                    if (!naEtapa.Any())
                    {
                        return 0d;
                    }

                    return naEtapa.Average(n => (DateTime.UtcNow - n.CreatedAt).TotalDays);
                });

        var conversaoPorResponsavel = negociacoesPeriodo
            .Where(n => !n.Ativa && n.Etapa == NegotiationStage.EntregaDeChaves)
            .Select(n => n.Eventos
                .OrderByDescending(e => e.OcorridoEm)
                .FirstOrDefault()?.Responsavel ?? "Não informado")
            .GroupBy(responsavel => responsavel, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var model = new IndicatorsViewModel
        {
            PeriodoInicio = start,
            PeriodoFim = end,
            VacanciaPercentual = totalImoveis == 0 ? 0 : Math.Round((decimal)disponiveis / totalImoveis * 100, 2),
            TempoMedioNegociacaoDias = negociacoesPeriodo.Count == 0 ? 0 : negociacoesPeriodo.Average(n => (DateTime.UtcNow - n.CreatedAt).TotalDays),
            TempoMedioVistoriaDias = vistorias.Count == 0 ? 0 : vistorias.Average(v => (v.Fim!.Value - v.Inicio!.Value).TotalDays),
            TempoMedioManutencaoDias = manutencoesComDatas.Count == 0 ? 0 : manutencoesComDatas.Average(m => (m.DataConclusao!.Value - m.IniciadaEm!.Value).TotalDays),
            CustoManutencaoPeriodo = manutencoes.Sum(m => m.CustoReal ?? 0),
            PendenciasCriticasAbertas = pendenciasCriticas,
            FinanceiroPendente = financeiro.Where(f => f.Status == FinancialStatus.Pendente).Sum(f => f.Valor),
            FinanceiroRecebido = financeiro.Where(f => f.Status == FinancialStatus.Recebido).Sum(f => f.Valor),
            TempoMedioPorEtapa = tempoMedioPorEtapa,
            ConversaoPorResponsavel = conversaoPorResponsavel,
            NegociacoesPorEtapa = negociacoesPorEtapa.ToDictionary(g => g.Etapa, g => g.Total),
            PendenciasPorSetor = pendenciasPorSetor.ToDictionary(g => g.Setor, g => g.Total, StringComparer.OrdinalIgnoreCase)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<FileResult> ExportarCsv([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken cancellationToken)
    {
        var start = inicio ?? DateTime.UtcNow.AddMonths(-1);
        var end = fim ?? DateTime.UtcNow;
        var atividades = await _context.Atividades
            .Where(a => a.CreatedAt >= start && a.CreatedAt <= end)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Id;Tipo;Titulo;Responsavel;Prioridade;Status;DataLimite");
        foreach (var atividade in atividades)
        {
            builder.AppendLine(string.Join(';',
                atividade.Id,
                atividade.Tipo,
                atividade.Titulo.Replace(';', ','),
                atividade.Responsavel,
                atividade.Prioridade,
                atividade.Status,
                atividade.DataLimite?.ToString("s", CultureInfo.InvariantCulture) ?? string.Empty));
        }

        return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"atividades-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarHtml([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken cancellationToken)
    {
        var start = inicio ?? DateTime.UtcNow.AddMonths(-1);
        var end = fim ?? DateTime.UtcNow;
        var vistorias = await _context.Vistorias
            .Include(v => v.Imovel)
            .Where(v => v.AgendadaPara >= start && v.AgendadaPara <= end)
            .OrderBy(v => v.AgendadaPara)
            .ToListAsync(cancellationToken);

        ViewData["Titulo"] = "Relatório de Vistorias";
        ViewData["Periodo"] = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}";
        return View("ExportarHtml", vistorias);
    }
}
