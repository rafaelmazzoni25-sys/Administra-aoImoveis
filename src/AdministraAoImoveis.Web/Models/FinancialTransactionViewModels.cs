using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class FinancialTransactionListViewModel
{
    public Guid NegociacaoId { get; set; }
    public string NegociacaoCodigo { get; set; } = string.Empty;
    public string Imovel { get; set; } = string.Empty;
    public string Interessado { get; set; } = string.Empty;
    public NegotiationStage Etapa { get; set; }
    public bool NegociacaoAtiva { get; set; }
    public decimal TotalPrevisto { get; set; }
    public decimal TotalRecebido { get; set; }
    public decimal TotalDevolvido { get; set; }
    public decimal TotalPendente { get; set; }
    public IReadOnlyList<FinancialTransactionItemViewModel> Lancamentos { get; set; } = Array.Empty<FinancialTransactionItemViewModel>();
    public FinancialTransactionInputModel NovoLancamento { get; set; } = new();
}

public class FinancialTransactionItemViewModel
{
    public Guid Id { get; set; }
    public string TipoLancamento { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public FinancialStatus Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public string CriadoPor { get; set; } = string.Empty;
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
    public DateTime? DataPrevista { get; set; }
    public DateTime? DataEfetivacao { get; set; }
    public string Observacao { get; set; } = string.Empty;
    public IReadOnlyCollection<FinancialStatus> TransicoesPermitidas { get; set; } = Array.Empty<FinancialStatus>();
    public FinancialTransactionStatusUpdateInputModel Atualizacao { get; set; } = new();
    public IReadOnlyCollection<string> ErrosAtualizacao { get; set; } = Array.Empty<string>();
}

public class FinancialTransactionInputModel
{
    [Required(ErrorMessage = "Informe o tipo do lançamento.")]
    [StringLength(120, ErrorMessage = "O tipo deve ter até {1} caracteres.")]
    public string TipoLancamento { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Informe um valor positivo.")]
    public decimal Valor { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DataPrevista { get; set; }

    [StringLength(2000, ErrorMessage = "A observação deve ter até {1} caracteres.")]
    public string? Observacao { get; set; }
}

public class FinancialTransactionStatusUpdateInputModel
{
    public Guid LancamentoId { get; set; }

    public FinancialStatus NovoStatus { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DataEfetivacao { get; set; }

    [StringLength(2000, ErrorMessage = "A justificativa deve ter até {1} caracteres.")]
    public string? Justificativa { get; set; }
}
