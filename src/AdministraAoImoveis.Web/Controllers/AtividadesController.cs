using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class AtividadesController : Controller
{
    private readonly ApplicationDbContext _context;

    public AtividadesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index([FromQuery] ActivityStatus? status, [FromQuery] PriorityLevel? prioridade, [FromQuery] string? responsavel, CancellationToken cancellationToken)
    {
        var query = _context.Atividades.Include(a => a.Comentarios).AsQueryable();

        if (status.HasValue)
        {
            var filtro = status.Value;
            query = query.Where(a => a.Status == filtro);
        }

        if (prioridade.HasValue)
        {
            var filtro = prioridade.Value;
            query = query.Where(a => a.Prioridade == filtro);
        }

        if (!string.IsNullOrWhiteSpace(responsavel))
        {
            query = query.Where(a => a.Responsavel == responsavel);
        }

        var atividades = await query
            .OrderByDescending(a => a.Prioridade)
            .ThenBy(a => a.DataLimite)
            .ToListAsync(cancellationToken);

        var model = new ActivityListViewModel
        {
            Status = status,
            Prioridade = prioridade,
            Responsavel = responsavel,
            Atividades = atividades
        };

        return View(model);
    }
}
