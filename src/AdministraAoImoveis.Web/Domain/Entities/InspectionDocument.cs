namespace AdministraAoImoveis.Web.Domain.Entities;

public class InspectionDocument : BaseEntity
{
    public Guid VistoriaId { get; set; }
    public Inspection? Vistoria { get; set; }
    public Guid ArquivoId { get; set; }
    public StoredFile? Arquivo { get; set; }
    public string Tipo { get; set; } = string.Empty;
}
