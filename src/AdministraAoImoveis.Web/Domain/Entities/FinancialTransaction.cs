using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class FinancialTransaction : BaseEntity
{
    public Guid NegociacaoId { get; set; }
    public Negotiation? Negociacao { get; set; }
    public string TipoLancamento { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public FinancialStatus Status { get; set; } = FinancialStatus.Pendente;
    public DateTime? DataPrevista { get; set; }
    public DateTime? DataEfetivacao { get; set; }
    public string Observacao { get; set; } = string.Empty;
}
