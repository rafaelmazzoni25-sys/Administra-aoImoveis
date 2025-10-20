using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class Inspection : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Property? Imovel { get; set; }
    public InspectionType Tipo { get; set; }
    public InspectionStatus Status { get; set; } = InspectionStatus.Agendada;
    public DateTime AgendadaPara { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public string ChecklistJson { get; set; } = "{}";
    public string Observacoes { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public ICollection<Activity> Atividades { get; set; } = new List<Activity>();
    public ICollection<InspectionDocument> Documentos { get; set; } = new List<InspectionDocument>();
    public ICollection<MaintenanceOrder> OrdensManutencao { get; set; } = new List<MaintenanceOrder>();
}
