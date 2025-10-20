using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class ImoveisController : Controller
{
    private readonly ApplicationDbContext _context;

    public ImoveisController(ApplicationDbContext context)
    {
        _context = context;
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
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (property is null)
        {
            return NotFound();
        }

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
                .ToList()
        };

        return View(model);
    }
}
