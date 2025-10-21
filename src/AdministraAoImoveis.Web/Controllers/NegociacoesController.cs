using System.Text.Json;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdministraAoImoveis.Web.Services;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize]
public class NegociacoesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrailService;

    public NegociacoesController(ApplicationDbContext context, IAuditTrailService auditTrailService)
    {
        _context = context;
        _auditTrailService = auditTrailService;
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

        var resultado = await AtualizarNegociacaoAsync(negotiation, novaEtapa, valorSinal, reservadoAte, cancellationToken);

        if (!resultado.Sucesso)
        {
            TempData["Error"] = resultado.Mensagem;
            return RedirectToAction(nameof(Index));
        }

        await RegistrarAuditoriaAsync(negotiation, "STAGE_UPDATE", cancellationToken, antes: resultado.Antes, depois: JsonSerializer.Serialize(negotiation));

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
        await RegistrarAuditoriaAsync(negotiation, concluida ? "COMPLETE" : "CANCEL", cancellationToken, antes: null, depois: JsonSerializer.Serialize(negotiation));

        TempData["Success"] = concluida
            ? "Negociação concluída e imóvel marcado como ocupado."
            : "Negociação cancelada e imóvel liberado novamente.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AtualizarQuadro([FromBody] NegotiationStageUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var negotiation = await _context.Negociacoes
            .Include(n => n.Imovel)
            .FirstOrDefaultAsync(n => n.Id == request.NegotiationId, cancellationToken);

        if (negotiation is null)
        {
            return NotFound();
        }

        var resultado = await AtualizarNegociacaoAsync(negotiation, request.Stage, request.ValorSinal, request.ReservadoAte, cancellationToken);
        if (!resultado.Sucesso)
        {
            return BadRequest(new { erro = resultado.Mensagem });
        }

        await RegistrarAuditoriaAsync(negotiation, "STAGE_UPDATE", cancellationToken, antes: resultado.Antes, depois: JsonSerializer.Serialize(negotiation));
        return Ok(new { mensagem = "Etapa atualizada." });
    }

    private async Task<(bool Sucesso, string Mensagem, string Antes)> AtualizarNegociacaoAsync(Domain.Entities.Negotiation negotiation, NegotiationStage novaEtapa, decimal? valorSinal, DateTime? reservadoAte, CancellationToken cancellationToken)
    {
        if (!negotiation.Ativa)
        {
            return (false, "A negociação está encerrada.", string.Empty);
        }

        if (valorSinal.HasValue && !reservadoAte.HasValue)
        {
            return (false, "Informe a validade da reserva quando houver valor de sinal.", string.Empty);
        }

        if (reservadoAte.HasValue && reservadoAte <= DateTime.UtcNow)
        {
            return (false, "A reserva deve ser futura.", string.Empty);
        }

        var possuiOutraAtiva = await _context.Negociacoes
            .AnyAsync(n => n.ImovelId == negotiation.ImovelId && n.Ativa && n.Id != negotiation.Id, cancellationToken);

        if (possuiOutraAtiva)
        {
            return (false, "Existe outra negociação ativa para o imóvel.", string.Empty);
        }

        var antes = JsonSerializer.Serialize(negotiation);

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

            negotiation.Imovel.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return (true, string.Empty, antes);
    }

    private async Task RegistrarAuditoriaAsync(Domain.Entities.Negotiation negotiation, string operacao, CancellationToken cancellationToken, string? antes, string? depois)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync("Negotiation", negotiation.Id, operacao, antes ?? string.Empty, depois ?? string.Empty, usuario, ip, host, cancellationToken);
    }
}
