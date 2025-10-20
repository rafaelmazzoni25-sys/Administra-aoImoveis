namespace AdministraAoImoveis.Web.Domain.Entities;

public class MaintenanceOrderDocument : BaseEntity
{
    public Guid OrdemManutencaoId { get; set; }
    public MaintenanceOrder? OrdemManutencao { get; set; }
    public Guid ArquivoId { get; set; }
    public StoredFile? Arquivo { get; set; }
    public string Categoria { get; set; } = string.Empty;
}
