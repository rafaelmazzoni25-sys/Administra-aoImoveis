using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBellViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return View(new NotificationBellViewModel());
        }

        var naoLidas = await _context.Notificacoes
            .Where(n => n.UsuarioId == userId && !n.Lida)
            .CountAsync(cancellationToken);

        var recentes = await _context.Notificacoes
            .Where(n => n.UsuarioId == userId)
            .OrderBy(n => n.Lida)
            .ThenByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new NotificationBellItemViewModel
            {
                Titulo = n.Titulo,
                Mensagem = n.Mensagem,
                LinkDestino = n.LinkDestino,
                Lida = n.Lida
            })
            .ToListAsync(cancellationToken);

        var model = new NotificationBellViewModel
        {
            NaoLidas = naoLidas,
            Recentes = recentes
        };

        return View(model);
    }
}
