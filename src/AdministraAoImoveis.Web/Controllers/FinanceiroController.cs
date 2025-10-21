using AdministraAoImoveis.Web.Data;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministraAoImoveis.Web.Controllers;

[Authorize(Roles = RoleNames.FinanceiroEquipe)]
[Route("Negociacoes/{negotiationId:guid}/Financeiro")]
public class FinanceiroController : Controller
{
    private static readonly FinancialStatus[] FinalStatuses =
    {
        FinancialStatus.Recebido,
        FinancialStatus.Devolvido,
        FinancialStatus.Cancelado
    };

    private static readonly IReadOnlyDictionary<FinancialStatus, FinancialStatus[]> AllowedTransitions = new Dictionary<FinancialStatus, FinancialStatus[]>
    {
        [FinancialStatus.Pendente] = new[] { FinancialStatus.Recebido, FinancialStatus.Devolvido, FinancialStatus.Cancelado },
        [FinancialStatus.Recebido] = new[] { FinancialStatus.Devolvido },
        [FinancialStatus.Devolvido] = Array.Empty<FinancialStatus>(),
        [FinancialStatus.Cancelado] = Array.Empty<FinancialStatus>()
    };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<FinanceiroController> _logger;

    public FinanceiroController(ApplicationDbContext context, ILogger<FinanceiroController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid negotiationId, CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(negotiationId, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("criar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Guid negotiationId, FinancialTransactionInputModel input, CancellationToken cancellationToken)
    {
        var negotiation = await LoadNegotiationAsync(negotiationId, cancellationToken);
        if (negotiation is null)
        {
            return NotFound();
        }

        if (input.DataPrevista.HasValue && input.DataPrevista.Value.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(FinancialTransactionInputModel.DataPrevista), "A data prevista não pode estar no passado.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildViewModelAsync(negotiationId, cancellationToken, input);
            return View("Index", invalidModel!);
        }

        var user = User?.Identity?.Name ?? "Sistema";
        var agora = DateTime.UtcNow;
        var tipo = input.TipoLancamento.Trim();
        var observacao = string.IsNullOrWhiteSpace(input.Observacao)
            ? string.Empty
            : input.Observacao.Trim();

        var lancamento = new FinancialTransaction
        {
            NegociacaoId = negotiation.Id,
            TipoLancamento = tipo,
            Valor = decimal.Round(input.Valor, 2, MidpointRounding.AwayFromZero),
            Status = FinancialStatus.Pendente,
            DataPrevista = input.DataPrevista,
            Observacao = observacao,
            CreatedBy = user,
            CreatedAt = agora
        };

        negotiation.LancamentosFinanceiros.Add(lancamento);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Lançamento financeiro {LancamentoId} criado para a negociação {NegociacaoId} pelo usuário {Usuario}",
            lancamento.Id,
            negotiation.Id,
            user);

        TempData["Success"] = "Lançamento financeiro registrado com sucesso.";
        return RedirectToAction(nameof(Index), new { negotiationId });
    }

    [HttpPost("{transactionId:guid}/atualizar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Atualizar(
        Guid negotiationId,
        Guid transactionId,
        FinancialTransactionStatusUpdateInputModel input,
        CancellationToken cancellationToken)
    {
        var negotiation = await LoadNegotiationAsync(negotiationId, cancellationToken);
        if (negotiation is null)
        {
            return NotFound();
        }

        if (input.LancamentoId == Guid.Empty)
        {
            input.LancamentoId = transactionId;
        }

        if (input.LancamentoId != transactionId)
        {
            TempData["Error"] = "Identificador do lançamento inválido.";
            return RedirectToAction(nameof(Index), new { negotiationId });
        }

        var lancamento = negotiation.LancamentosFinanceiros.FirstOrDefault(l => l.Id == transactionId);
        if (lancamento is null)
        {
            return NotFound();
        }

        var erros = ValidateStatusUpdate(lancamento, input);
        if (erros.Count > 0)
        {
            var invalidModel = await BuildViewModelAsync(negotiationId, cancellationToken, null, (input, erros));
            return View("Index", invalidModel!);
        }

        var user = User?.Identity?.Name ?? "Sistema";
        var agora = DateTime.UtcNow;

        lancamento.Status = input.NovoStatus;
        lancamento.DataEfetivacao = FinalStatuses.Contains(input.NovoStatus)
            ? input.DataEfetivacao ?? agora.Date
            : null;
        lancamento.UpdatedAt = agora;
        lancamento.UpdatedBy = user;

        if (!string.IsNullOrWhiteSpace(input.Justificativa))
        {
            var justificativa = input.Justificativa.Trim();
            var anotacao = $"[{agora:dd/MM/yyyy HH:mm}] {user}: {justificativa}";
            lancamento.Observacao = string.IsNullOrWhiteSpace(lancamento.Observacao)
                ? anotacao
                : string.Join(Environment.NewLine + Environment.NewLine, lancamento.Observacao.Trim(), anotacao);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Status do lançamento {LancamentoId} atualizado para {Status} na negociação {NegociacaoId}.",
            lancamento.Id,
            lancamento.Status,
            negotiation.Id);

        TempData["Success"] = "Status do lançamento atualizado com sucesso.";
        return RedirectToAction(nameof(Index), new { negotiationId });
    }

    private async Task<Negotiation?> LoadNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken)
    {
        return await _context.Negociacoes
            .Include(n => n.Imovel)
            .Include(n => n.Interessado)
            .Include(n => n.LancamentosFinanceiros)
            .FirstOrDefaultAsync(n => n.Id == negotiationId, cancellationToken);
    }

    private async Task<FinancialTransactionListViewModel?> BuildViewModelAsync(
        Guid negotiationId,
        CancellationToken cancellationToken,
        FinancialTransactionInputModel? novoLancamento = null,
        (FinancialTransactionStatusUpdateInputModel Input, IReadOnlyCollection<string> Errors)? atualizacao = null)
    {
        var negotiation = await _context.Negociacoes
            .AsNoTracking()
            .Include(n => n.Imovel)
            .Include(n => n.Interessado)
            .Include(n => n.LancamentosFinanceiros)
            .FirstOrDefaultAsync(n => n.Id == negotiationId, cancellationToken);

        if (negotiation is null)
        {
            return null;
        }

        var totalPrevisto = negotiation.LancamentosFinanceiros.Sum(l => l.Valor);
        var totalRecebido = negotiation.LancamentosFinanceiros
            .Where(l => l.Status == FinancialStatus.Recebido)
            .Sum(l => l.Valor);
        var totalDevolvido = negotiation.LancamentosFinanceiros
            .Where(l => l.Status == FinancialStatus.Devolvido)
            .Sum(l => l.Valor);
        var totalPendente = negotiation.LancamentosFinanceiros
            .Where(l => l.Status == FinancialStatus.Pendente)
            .Sum(l => l.Valor);

        var itens = negotiation.LancamentosFinanceiros
            .OrderBy(l => l.DataPrevista ?? l.CreatedAt)
            .ThenBy(l => l.CreatedAt)
            .Select(l => MapTransaction(l, atualizacao))
            .ToList();

        return new FinancialTransactionListViewModel
        {
            NegociacaoId = negotiation.Id,
            NegociacaoCodigo = negotiation.Imovel?.CodigoInterno ?? negotiation.Id.ToString()[..8],
            Imovel = negotiation.Imovel is null
                ? "Imóvel não informado"
                : $"{negotiation.Imovel.CodigoInterno} - {negotiation.Imovel.Titulo}",
            Interessado = negotiation.Interessado?.Nome ?? "Interessado não identificado",
            Etapa = negotiation.Etapa,
            NegociacaoAtiva = negotiation.Ativa,
            TotalPrevisto = totalPrevisto,
            TotalRecebido = totalRecebido,
            TotalDevolvido = totalDevolvido,
            TotalPendente = totalPendente,
            Lancamentos = itens,
            NovoLancamento = novoLancamento ?? new FinancialTransactionInputModel()
        };
    }

    private FinancialTransactionItemViewModel MapTransaction(
        FinancialTransaction transaction,
        (FinancialTransactionStatusUpdateInputModel Input, IReadOnlyCollection<string> Errors)? atualizacao)
    {
        var transicoes = AllowedTransitions.TryGetValue(transaction.Status, out var allowed)
            ? allowed
            : Array.Empty<FinancialStatus>();

        FinancialTransactionStatusUpdateInputModel atualizacaoModelo;
        IReadOnlyCollection<string> erros = Array.Empty<string>();

        if (atualizacao.HasValue && atualizacao.Value.Input.LancamentoId == transaction.Id)
        {
            atualizacaoModelo = new FinancialTransactionStatusUpdateInputModel
            {
                LancamentoId = transaction.Id,
                NovoStatus = atualizacao.Value.Input.NovoStatus,
                DataEfetivacao = atualizacao.Value.Input.DataEfetivacao,
                Justificativa = atualizacao.Value.Input.Justificativa
            };
            erros = atualizacao.Value.Errors;
        }
        else
        {
            atualizacaoModelo = new FinancialTransactionStatusUpdateInputModel
            {
                LancamentoId = transaction.Id,
                NovoStatus = transicoes.FirstOrDefault(),
                DataEfetivacao = transaction.DataEfetivacao,
                Justificativa = string.Empty
            };
        }

        if (atualizacaoModelo.NovoStatus == default && transicoes.Length > 0)
        {
            atualizacaoModelo.NovoStatus = transicoes[0];
        }

        if (atualizacaoModelo.DataEfetivacao is null && transicoes.Any(t => t is FinancialStatus.Recebido or FinancialStatus.Devolvido))
        {
            atualizacaoModelo.DataEfetivacao = DateTime.UtcNow.Date;
        }

        return new FinancialTransactionItemViewModel
        {
            Id = transaction.Id,
            TipoLancamento = transaction.TipoLancamento,
            Valor = transaction.Valor,
            Status = transaction.Status,
            CriadoEm = transaction.CreatedAt,
            CriadoPor = string.IsNullOrWhiteSpace(transaction.CreatedBy) ? "Sistema" : transaction.CreatedBy,
            AtualizadoEm = transaction.UpdatedAt,
            AtualizadoPor = transaction.UpdatedBy,
            DataPrevista = transaction.DataPrevista,
            DataEfetivacao = transaction.DataEfetivacao,
            Observacao = transaction.Observacao,
            TransicoesPermitidas = transicoes,
            Atualizacao = atualizacaoModelo,
            ErrosAtualizacao = erros
        };
    }

    private static IReadOnlyCollection<string> ValidateStatusUpdate(
        FinancialTransaction transaction,
        FinancialTransactionStatusUpdateInputModel input)
    {
        var erros = new List<string>();

        if (!AllowedTransitions.TryGetValue(transaction.Status, out var permitidos) || permitidos.Length == 0)
        {
            erros.Add("O lançamento não permite novas alterações de status.");
            return erros;
        }

        if (!permitidos.Contains(input.NovoStatus))
        {
            erros.Add("Status selecionado não é válido para o estado atual do lançamento.");
        }

        if ((input.NovoStatus == FinancialStatus.Recebido || input.NovoStatus == FinancialStatus.Devolvido) && !input.DataEfetivacao.HasValue)
        {
            erros.Add("Informe a data de efetivação para lançamentos recebidos ou devolvidos.");
        }

        if (input.DataEfetivacao.HasValue && input.DataEfetivacao.Value.Date > DateTime.UtcNow.Date)
        {
            erros.Add("A data de efetivação não pode estar no futuro.");
        }

        if ((input.NovoStatus == FinancialStatus.Devolvido || input.NovoStatus == FinancialStatus.Cancelado) && string.IsNullOrWhiteSpace(input.Justificativa))
        {
            erros.Add("Informe a justificativa ao marcar um lançamento como devolvido ou cancelado.");
        }

        if (input.Justificativa?.Length > 2000)
        {
            erros.Add("A justificativa deve ter até 2000 caracteres.");
        }

        return erros;
    }
}
