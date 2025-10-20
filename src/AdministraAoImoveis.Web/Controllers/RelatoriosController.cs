using System.Globalization;
using System.Text;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class RelatoriosController : Controller
{
    private readonly ApplicationDbContext _context;

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
        var negociacoes = await _context.Negociacoes
            .Where(n => n.CreatedAt >= start && n.CreatedAt <= end)
            .ToListAsync(cancellationToken);
        var vistorias = await _context.Vistorias
            .Where(v => v.Inicio != null && v.Fim != null && v.Inicio >= start && v.Fim <= end)
            .ToListAsync(cancellationToken);
        var manutencoes = await _context.Manutencoes
            .Where(m => m.DataConclusao != null && m.DataConclusao >= start && m.DataConclusao <= end && m.CustoReal.HasValue)
            .ToListAsync(cancellationToken);

        var model = new IndicatorsViewModel
        {
            PeriodoInicio = start,
            PeriodoFim = end,
            VacanciaPercentual = totalImoveis == 0 ? 0 : Math.Round((decimal)disponiveis / totalImoveis * 100, 2),
            TempoMedioNegociacaoDias = negociacoes.Count == 0 ? 0 : negociacoes.Average(n => (DateTime.UtcNow - n.CreatedAt).TotalDays),
            TempoMedioVistoriaDias = vistorias.Count == 0 ? 0 : vistorias.Average(v => (v.Fim!.Value - v.Inicio!.Value).TotalDays),
            CustoManutencaoPeriodo = manutencoes.Sum(m => m.CustoReal ?? 0)
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
