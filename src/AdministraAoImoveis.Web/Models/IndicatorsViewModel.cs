namespace AdministraAoImoveis.Web.Models;

public class IndicatorsViewModel
{
    public decimal VacanciaPercentual { get; set; }
    public double TempoMedioNegociacaoDias { get; set; }
    public double TempoMedioVistoriaDias { get; set; }
    public decimal CustoManutencaoPeriodo { get; set; }
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFim { get; set; }
}
