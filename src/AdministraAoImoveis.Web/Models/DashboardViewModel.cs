namespace AdministraAoImoveis.Web.Models;

public class DashboardViewModel
{
    public int ImoveisDisponiveis { get; set; }
    public int ImoveisEmNegociacao { get; set; }
    public int PendenciasCriticas { get; set; }
    public int VistoriasPendentes { get; set; }
}
