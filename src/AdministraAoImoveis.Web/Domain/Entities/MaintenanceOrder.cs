using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class MaintenanceOrder : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Property? Imovel { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public MaintenanceOrderStatus Status { get; set; } = MaintenanceOrderStatus.Solicitada;
    public DateTime? PrevisaoConclusao { get; set; }
    public DateTime? IniciadaEm { get; set; }
    public decimal? CustoEstimado { get; set; }
    public decimal? CustoReal { get; set; }
    public DateTime? DataConclusao { get; set; }
    public string Responsavel { get; set; } = string.Empty;
    public string Contato { get; set; } = string.Empty;
    public AvailabilityStatus? StatusDisponibilidadeAnterior { get; set; }
    public Guid? VistoriaId { get; set; }
    public Inspection? Vistoria { get; set; }
    public ICollection<MaintenanceOrderDocument> Documentos { get; set; } = new List<MaintenanceOrderDocument>();
}
