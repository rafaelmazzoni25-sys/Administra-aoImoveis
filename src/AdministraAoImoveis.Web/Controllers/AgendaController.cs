using AdministraAoImoveis.Web.Data;
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

    public async Task<IActionResult> Index([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, CancellationToken cancellationToken)
    {
        var start = inicio ?? DateTime.UtcNow.Date;
        var end = fim ?? start.AddDays(7);

        var compromissos = await _context.Agenda
            .Where(a => a.Inicio >= start && a.Inicio <= end)
            .OrderBy(a => a.Inicio)
            .ToListAsync(cancellationToken);

        var model = new ScheduleCalendarViewModel
        {
            Inicio = start,
            Fim = end,
            Compromissos = compromissos
        };

        return View(model);
    }
}
