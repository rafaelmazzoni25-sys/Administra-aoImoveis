using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class Negotiation : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Property? Imovel { get; set; }
    public Guid InteressadoId { get; set; }
    public InterestedParty? Interessado { get; set; }
    public NegotiationStage Etapa { get; set; } = NegotiationStage.LeadCaptado;
    public bool Ativa { get; set; } = true;
    public decimal? ValorProposta { get; set; }
    public decimal? ValorSinal { get; set; }
    public DateTime? ReservadoAte { get; set; }
    public string ObservacoesInternas { get; set; } = string.Empty;
    public ICollection<NegotiationEvent> Eventos { get; set; } = new List<NegotiationEvent>();
    public ICollection<Activity> Atividades { get; set; } = new List<Activity>();
    public ICollection<NegotiationDocument> Documentos { get; set; } = new List<NegotiationDocument>();
    public ICollection<FinancialTransaction> LancamentosFinanceiros { get; set; } = new List<FinancialTransaction>();
}
