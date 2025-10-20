using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Areas.PortalProprietario.Controllers;

[Area("PortalProprietario")]
[Authorize(Roles = "PROPRIETARIO")]
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
        var owner = await _context.Proprietarios
            .Include(p => p.Imoveis)
                .ThenInclude(i => i.Vistorias)
            .Include(p => p.Imoveis)
                .ThenInclude(i => i.Manutencoes)
            .FirstOrDefaultAsync(p => p.UsuarioId == userId, cancellationToken);

        if (owner is null)
        {
            return View(new OwnerPortalViewModel());
        }

        var model = new OwnerPortalViewModel
        {
            Nome = owner.Nome,
            Imoveis = owner.Imoveis.ToList(),
            Vistorias = owner.Imoveis.SelectMany(i => i.Vistorias).OrderByDescending(v => v.AgendadaPara).ToList(),
            Manutencoes = owner.Imoveis.SelectMany(i => i.Manutencoes).OrderByDescending(m => m.CreatedAt).ToList()
        };

        return View(model);
    }
}
