using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class IndicatorsViewModel
{
    public decimal VacanciaPercentual { get; set; }
    public double TempoMedioNegociacaoDias { get; set; }
    public double TempoMedioVistoriaDias { get; set; }
    public decimal CustoManutencaoPeriodo { get; set; }
    public double TempoMedioManutencaoDias { get; set; }
    public IReadOnlyDictionary<NegotiationStage, double> TempoMedioPorEtapa { get; set; } = new Dictionary<NegotiationStage, double>();
    public IReadOnlyDictionary<string, int> ConversaoPorResponsavel { get; set; } = new Dictionary<string, int>();
    public int PendenciasCriticasAbertas { get; set; }
    public decimal FinanceiroPendente { get; set; }
    public decimal FinanceiroRecebido { get; set; }
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFim { get; set; }
}
