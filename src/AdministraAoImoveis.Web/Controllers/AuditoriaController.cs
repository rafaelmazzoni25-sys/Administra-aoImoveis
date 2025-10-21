using System.Text;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class AuditoriaController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditoriaController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] string? entidade,
        [FromQuery] string? usuario,
        CancellationToken cancellationToken)
    {
        var (start, end) = NormalizarPeriodo(inicio, fim);

        var query = AplicarFiltros(_context.AuditTrail.AsNoTracking(), start, end, entidade, usuario);
        var registros = await query
            .OrderByDescending(a => a.RegistradoEm)
            .Take(500)
            .ToListAsync(cancellationToken);

        var entidadesDisponiveis = await _context.AuditTrail
            .AsNoTracking()
            .Select(a => a.Entidade)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync(cancellationToken);

        var model = new AuditLogListViewModel
        {
            Inicio = start,
            Fim = end,
            Entidade = string.IsNullOrWhiteSpace(entidade) ? null : entidade,
            Usuario = string.IsNullOrWhiteSpace(usuario) ? null : usuario,
            EntidadesDisponiveis = entidadesDisponiveis,
            Registros = registros
        };

        return View(model);
    }

    [HttpGet]
    public async Task<FileResult> ExportarCsv(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] string? entidade,
        [FromQuery] string? usuario,
        CancellationToken cancellationToken)
    {
        var (start, end) = NormalizarPeriodo(inicio, fim);
        var registros = await AplicarFiltros(_context.AuditTrail.AsNoTracking(), start, end, entidade, usuario)
            .OrderByDescending(a => a.RegistradoEm)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("RegistradoEm;Entidade;EntidadeId;Operacao;Usuario;Ip;Host");
        foreach (var entry in registros)
        {
            builder.AppendLine(string.Join(';',
                entry.RegistradoEm.ToString("s"),
                entry.Entidade,
                entry.EntidadeId,
                entry.Operacao,
                entry.Usuario,
                entry.Ip,
                entry.Host));
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return File(bytes, "text/csv", $"auditoria-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarHtml(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] string? entidade,
        [FromQuery] string? usuario,
        CancellationToken cancellationToken)
    {
        var (start, end) = NormalizarPeriodo(inicio, fim);
        var registros = await AplicarFiltros(_context.AuditTrail.AsNoTracking(), start, end, entidade, usuario)
            .OrderByDescending(a => a.RegistradoEm)
            .ToListAsync(cancellationToken);

        ViewData["Titulo"] = "Relatório de Auditoria";
        ViewData["Periodo"] = $"{start:dd/MM/yyyy HH:mm} - {end:dd/MM/yyyy HH:mm}";
        return View(registros);
    }

    private static (DateTime Inicio, DateTime Fim) NormalizarPeriodo(DateTime? inicio, DateTime? fim)
    {
        var start = inicio ?? DateTime.UtcNow.AddDays(-7);
        var end = fim ?? DateTime.UtcNow;

        if (end <= start)
        {
            end = start.AddDays(1);
        }

        return (start, end);
    }

    private static IQueryable<AuditLogEntry> AplicarFiltros(
        IQueryable<AuditLogEntry> query,
        DateTime inicio,
        DateTime fim,
        string? entidade,
        string? usuario)
    {
        var startUtc = inicio.Kind == DateTimeKind.Utc ? inicio : DateTime.SpecifyKind(inicio, DateTimeKind.Utc);
        var endUtc = fim.Kind == DateTimeKind.Utc ? fim : DateTime.SpecifyKind(fim, DateTimeKind.Utc);

        query = query.Where(a => a.RegistradoEm >= startUtc && a.RegistradoEm <= endUtc);

        if (!string.IsNullOrWhiteSpace(entidade))
        {
            query = query.Where(a => a.Entidade == entidade);
        }

        if (!string.IsNullOrWhiteSpace(usuario))
        {
            query = query.Where(a => a.Usuario == usuario);
        }

        return query;
    }
}
