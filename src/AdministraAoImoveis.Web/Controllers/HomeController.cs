using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = new DashboardViewModel
        {
            ImoveisDisponiveis = await _context.Imoveis.CountAsync(p => p.StatusDisponibilidade == Domain.Enumerations.AvailabilityStatus.Disponivel, cancellationToken),
            ImoveisEmNegociacao = await _context.Imoveis.CountAsync(p => p.StatusDisponibilidade == Domain.Enumerations.AvailabilityStatus.EmNegociacao, cancellationToken),
            PendenciasCriticas = await _context.Atividades.CountAsync(a => a.Prioridade == Domain.Enumerations.PriorityLevel.Critica && a.Status != Domain.Enumerations.ActivityStatus.Concluida, cancellationToken),
            VistoriasPendentes = await _context.Vistorias.CountAsync(v => v.Status != Domain.Enumerations.InspectionStatus.Concluida, cancellationToken)
        };

        return View(dashboard);
    }
}
