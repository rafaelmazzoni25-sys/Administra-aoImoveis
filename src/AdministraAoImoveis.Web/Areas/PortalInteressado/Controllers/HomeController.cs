using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Areas.PortalInteressado.Controllers;

[Area("PortalInteressado")]
[Authorize(Roles = "INTERESSADO")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        var applicant = await _context.Interessados
            .Include(i => i.Negociacoes)
                .ThenInclude(n => n.Imovel)
            .Include(i => i.Negociacoes)
                .ThenInclude(n => n.Eventos)
            .FirstOrDefaultAsync(i => i.UsuarioId == userId, cancellationToken);

        if (applicant is null)
        {
            return View(new ApplicantPortalViewModel());
        }

        var model = new ApplicantPortalViewModel
        {
            Nome = applicant.Nome,
            Negociacoes = applicant.Negociacoes
                .OrderByDescending(n => n.CreatedAt)
                .ToList()
        };

        return View(model);
    }
}
