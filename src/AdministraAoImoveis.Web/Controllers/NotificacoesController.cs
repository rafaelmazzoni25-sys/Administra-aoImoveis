using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class NotificacoesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificacoesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var notificacoes = await _context.Notificacoes
            .Where(n => n.UsuarioId == userId)
            .OrderBy(n => n.Lida)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        var model = new NotificationListViewModel
        {
            Notificacoes = notificacoes
                .Select(n => new NotificationItemViewModel
                {
                    Id = n.Id,
                    Titulo = n.Titulo,
                    Mensagem = n.Mensagem,
                    Lida = n.Lida,
                    CreatedAt = n.CreatedAt,
                    LidaEm = n.LidaEm,
                    LinkDestino = n.LinkDestino
                })
                .ToList(),
            TotalNaoLidas = notificacoes.Count(n => !n.Lida)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarComoLida(Guid id, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var notificacao = await _context.Notificacoes
            .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == userId, cancellationToken);

        if (notificacao is null)
        {
            return NotFound();
        }

        if (!notificacao.Lida)
        {
            notificacao.Lida = true;
            notificacao.LidaEm = DateTime.UtcNow;
            notificacao.UpdatedAt = DateTime.UtcNow;
            notificacao.UpdatedBy = User?.Identity?.Name ?? "Sistema";
            await _context.SaveChangesAsync(cancellationToken);
        }

        TempData["Success"] = "Notificação atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarTodas(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var pendentes = await _context.Notificacoes
            .Where(n => n.UsuarioId == userId && !n.Lida)
            .ToListAsync(cancellationToken);

        if (pendentes.Count > 0)
        {
            var usuario = User?.Identity?.Name ?? "Sistema";
            foreach (var notificacao in pendentes)
            {
                notificacao.Lida = true;
                notificacao.LidaEm = DateTime.UtcNow;
                notificacao.UpdatedAt = DateTime.UtcNow;
                notificacao.UpdatedBy = usuario;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        TempData["Success"] = pendentes.Count == 0
            ? "Não havia notificações pendentes."
            : "Notificações marcadas como lidas.";

        return RedirectToAction(nameof(Index));
    }
}
