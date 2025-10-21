using System.Globalization;
using System.Text.Json;
using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdministraAoImoveis.Web.Services;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize(Roles = RoleNames.Operacional)]
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
        await LiberarReservasExpiradasAsync(cancellationToken);
        var colunas = new Dictionary<NegotiationStage, IReadOnlyCollection<Domain.Entities.Negotiation>>();
        foreach (NegotiationStage stage in Enum.GetValues(typeof(NegotiationStage)))
        {
            var negotiations = await _context.Negociacoes
                .Where(n => n.Etapa == stage && n.Ativa)
                .Include(n => n.Imovel)
                .Include(n => n.Interessado)
                .Include(n => n.LancamentosFinanceiros)
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
            .Include(n => n.LancamentosFinanceiros)
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
        var descricao = BuildEventoDescricao(resultado, negotiation);
        if (!string.IsNullOrEmpty(descricao))
        {
            await RegistrarEventoAsync(negotiation, "Atualização de negociação", descricao, cancellationToken);
        }

        TempData["Success"] = "Negociação atualizada com sucesso.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Encerrar(Guid id, bool concluida, CancellationToken cancellationToken)
    {
        var negotiation = await _context.Negociacoes
            .Include(n => n.Imovel)
            .Include(n => n.LancamentosFinanceiros)
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

        if (concluida && negotiation.LancamentosFinanceiros.Any(l => l.Status == FinancialStatus.Pendente))
        {
            TempData["Error"] = "Conclua ou cancele os lançamentos financeiros antes de encerrar a negociação.";
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
        var descricao = concluida
            ? "Negociação concluída e contrato ativo gerado."
            : "Negociação cancelada e imóvel liberado.";
        await RegistrarEventoAsync(negotiation, concluida ? "Conclusão de negociação" : "Cancelamento de negociação", descricao, cancellationToken);

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
            .Include(n => n.LancamentosFinanceiros)
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
        var descricao = BuildEventoDescricao(resultado, negotiation);
        if (!string.IsNullOrEmpty(descricao))
        {
            await RegistrarEventoAsync(negotiation, "Atualização de negociação", descricao, cancellationToken);
        }
        return Ok(new { mensagem = "Etapa atualizada." });
    }

    private async Task<NegotiationUpdateResult> AtualizarNegociacaoAsync(Negotiation negotiation, NegotiationStage novaEtapa, decimal? valorSinal, DateTime? reservadoAte, CancellationToken cancellationToken)
    {
        if (!negotiation.Ativa)
        {
            return new NegotiationUpdateResult(false, "A negociação está encerrada.", string.Empty, null, null, null);
        }

        if (valorSinal.HasValue && !reservadoAte.HasValue)
        {
            return new NegotiationUpdateResult(false, "Informe a validade da reserva quando houver valor de sinal.", string.Empty, null, null, null);
        }

        if (reservadoAte.HasValue && reservadoAte <= DateTime.UtcNow)
        {
            return new NegotiationUpdateResult(false, "A reserva deve ser futura.", string.Empty, null, null, null);
        }

        var possuiOutraAtiva = await _context.Negociacoes
            .AnyAsync(n => n.ImovelId == negotiation.ImovelId && n.Ativa && n.Id != negotiation.Id, cancellationToken);

        if (possuiOutraAtiva)
        {
            return new NegotiationUpdateResult(false, "Existe outra negociação ativa para o imóvel.", string.Empty, null, null, null);
        }

        var possuiFinanceiroPendente = negotiation.LancamentosFinanceiros.Any(l => l.Status == FinancialStatus.Pendente);
        if (possuiFinanceiroPendente && novaEtapa >= NegotiationStage.ContratoEmitido)
        {
            return new NegotiationUpdateResult(false, "Existem pendências financeiras que impedem o avanço da negociação.", string.Empty, null, null, null);
        }

        var antes = JsonSerializer.Serialize(negotiation);
        var etapaAnterior = negotiation.Etapa;
        var valorSinalAnterior = negotiation.ValorSinal;
        var reservadoAteAnterior = negotiation.ReservadoAte;

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

        return new NegotiationUpdateResult(true, string.Empty, antes, etapaAnterior, valorSinalAnterior, reservadoAteAnterior);
    }

    private async Task RegistrarAuditoriaAsync(Domain.Entities.Negotiation negotiation, string operacao, CancellationToken cancellationToken, string? antes, string? depois)
    {
        var usuario = User?.Identity?.Name ?? "Sistema";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var host = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

        await _auditTrailService.RegisterAsync("Negotiation", negotiation.Id, operacao, antes ?? string.Empty, depois ?? string.Empty, usuario, ip, host, cancellationToken);
    }

    private string? BuildEventoDescricao(NegotiationUpdateResult resultado, Negotiation negotiation)
    {
        var partes = new List<string>();

        if (resultado.EtapaAnterior.HasValue && resultado.EtapaAnterior.Value != negotiation.Etapa)
        {
            partes.Add($"Etapa: {resultado.EtapaAnterior.Value} → {negotiation.Etapa}");
        }

        if (resultado.ValorSinalAnterior != negotiation.ValorSinal)
        {
            var cultura = CultureInfo.GetCultureInfo("pt-BR");
            var antes = resultado.ValorSinalAnterior.HasValue ? resultado.ValorSinalAnterior.Value.ToString("C", cultura) : "—";
            var depois = negotiation.ValorSinal.HasValue ? negotiation.ValorSinal.Value.ToString("C", cultura) : "—";
            partes.Add($"Sinal: {antes} → {depois}");
        }

        if (resultado.ReservadoAteAnterior != negotiation.ReservadoAte)
        {
            var antes = resultado.ReservadoAteAnterior.HasValue ? resultado.ReservadoAteAnterior.Value.ToString("dd/MM/yyyy HH:mm") : "sem data";
            var depois = negotiation.ReservadoAte.HasValue ? negotiation.ReservadoAte.Value.ToString("dd/MM/yyyy HH:mm") : "sem data";
            partes.Add($"Reserva até: {antes} → {depois}");
        }

        return partes.Count == 0 ? null : string.Join(" | ", partes);
    }

    private async Task RegistrarEventoAsync(Negotiation negotiation, string titulo, string descricao, CancellationToken cancellationToken)
    {
        var responsavel = User?.Identity?.Name ?? "Sistema";
        var evento = new NegotiationEvent
        {
            NegociacaoId = negotiation.Id,
            Titulo = titulo,
            Descricao = descricao,
            Responsavel = responsavel,
            OcorridoEm = DateTime.UtcNow,
            CreatedBy = responsavel
        };

        _context.NegociacaoEventos.Add(evento);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task LiberarReservasExpiradasAsync(CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var expiradas = await _context.Negociacoes
            .Include(n => n.Imovel)
            .Where(n => n.Ativa && n.ReservadoAte != null && n.ReservadoAte < agora)
            .ToListAsync(cancellationToken);

        if (expiradas.Count == 0)
        {
            return;
        }

        var auditorias = new List<(Domain.Entities.Negotiation Negociacao, string Antes, string Depois)>();

        foreach (var negotiation in expiradas)
        {
            var antes = JsonSerializer.Serialize(negotiation);
            negotiation.ReservadoAte = null;
            negotiation.ValorSinal = null;
            if (negotiation.Imovel is not null)
            {
                negotiation.Imovel.StatusDisponibilidade = AvailabilityStatus.EmNegociacao;
                negotiation.Imovel.DataPrevistaDisponibilidade = null;
                negotiation.Imovel.UpdatedAt = agora;
            }

            negotiation.UpdatedAt = agora;
            var depois = JsonSerializer.Serialize(negotiation);
            auditorias.Add((negotiation, antes, depois));
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (negociacao, antes, depois) in auditorias)
        {
            await RegistrarAuditoriaAsync(negociacao, "RESERVATION_EXPIRED", cancellationToken, antes, depois);
            await RegistrarEventoAsync(negociacao, "Reserva expirada", "Reserva encerrada automaticamente após o vencimento.", cancellationToken);
        }
    }

    private sealed record NegotiationUpdateResult(
        bool Sucesso,
        string Mensagem,
        string Antes,
        NegotiationStage? EtapaAnterior,
        decimal? ValorSinalAnterior,
        DateTime? ReservadoAteAnterior);
}
