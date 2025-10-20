using System;
using System.Linq;
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
            Colunas = colunas,
            Etapas = Enum.GetValues(typeof(NegotiationStage))
                .Cast<NegotiationStage>()
                .ToArray()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mover(Guid id, NegotiationStage novaEtapa, decimal? valorSinal, DateTime? reservadoAte, CancellationToken cancellationToken)
    {
        var negotiation = await _context.Negociacoes
            .Include(n => n.Imovel)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (negotiation is null)
        {
            return NotFound();
        }

        if (!negotiation.Ativa)
        {
            TempData["Error"] = "A negociação já está encerrada.";
            return RedirectToAction(nameof(Index));
        }

        if (valorSinal.HasValue && !reservadoAte.HasValue)
        {
            TempData["Error"] = "Informe a data limite da reserva quando houver valor de sinal.";
            return RedirectToAction(nameof(Index));
        }

        if (reservadoAte.HasValue && reservadoAte <= DateTime.UtcNow)
        {
            TempData["Error"] = "A data de reserva deve ser futura.";
            return RedirectToAction(nameof(Index));
        }

        var possuiOutraAtiva = await _context.Negociacoes
            .AnyAsync(n => n.ImovelId == negotiation.ImovelId && n.Ativa && n.Id != negotiation.Id, cancellationToken);

        if (possuiOutraAtiva)
        {
            TempData["Error"] = "Já existe outra negociação ativa para este imóvel. Encerre a negociação concorrente antes de prosseguir.";
            return RedirectToAction(nameof(Index));
        }

        negotiation.Etapa = novaEtapa;
        negotiation.ValorSinal = valorSinal;
        negotiation.ReservadoAte = reservadoAte;
        negotiation.UpdatedAt = DateTime.UtcNow;

        if (negotiation.Imovel is not null)
        {
            if (reservadoAte.HasValue || valorSinal.HasValue)
            {
                negotiation.Imovel.StatusDisponibilidade = AvailabilityStatus.Reservado;
                negotiation.Imovel.DataPrevistaDisponibilidade = reservadoAte;
            }
            else
            {
                negotiation.Imovel.StatusDisponibilidade = AvailabilityStatus.EmNegociacao;
                negotiation.Imovel.DataPrevistaDisponibilidade = null;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Negociação atualizada com sucesso.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Encerrar(Guid id, bool concluida, CancellationToken cancellationToken)
    {
        var negotiation = await _context.Negociacoes
            .Include(n => n.Imovel)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (negotiation is null)
        {
            return NotFound();
        }

        if (!negotiation.Ativa)
        {
            TempData["Error"] = "A negociação já está encerrada.";
            return RedirectToAction(nameof(Index));
        }

        negotiation.Ativa = false;
        negotiation.ReservadoAte = null;
        negotiation.ValorSinal = null;
        negotiation.UpdatedAt = DateTime.UtcNow;

        if (concluida)
        {
            negotiation.Etapa = NegotiationStage.EntregaDeChaves;
        }

        if (negotiation.Imovel is not null)
        {
            negotiation.Imovel.DataPrevistaDisponibilidade = null;
            negotiation.Imovel.StatusDisponibilidade = concluida
                ? AvailabilityStatus.Ocupado
                : AvailabilityStatus.Disponivel;
        }

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = concluida
            ? "Negociação concluída e imóvel marcado como ocupado."
            : "Negociação cancelada e imóvel liberado novamente.";

        return RedirectToAction(nameof(Index));
    }
}
