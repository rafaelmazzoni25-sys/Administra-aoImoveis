using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class NegociacoesController : Controller
{
    private readonly ApplicationDbContext _context;

    public NegociacoesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var colunas = new Dictionary<NegotiationStage, IReadOnlyCollection<Domain.Entities.Negotiation>>();
        foreach (NegotiationStage stage in Enum.GetValues(typeof(NegotiationStage)))
        {
            var negotiations = await _context.Negociacoes
                .Where(n => n.Etapa == stage && n.Ativa)
                .Include(n => n.Imovel)
                .Include(n => n.Interessado)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
            colunas[stage] = negotiations;
        }

        var model = new NegotiationBoardViewModel
        {
            Colunas = colunas
        };

        return View(model);
    }
}
