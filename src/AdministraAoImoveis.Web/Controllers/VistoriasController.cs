using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class VistoriasController : Controller
{
    private readonly ApplicationDbContext _context;

    public VistoriasController(ApplicationDbContext context)
    {
        _context = context;
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
            .ToListAsync(cancellationToken);

        var model = new InspectionListViewModel
        {
            Status = status,
            Tipo = tipo,
            Vistorias = vistorias
        };

        return View(model);
    }
}
