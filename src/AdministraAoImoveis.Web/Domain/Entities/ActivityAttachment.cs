namespace AdministraAoImoveis.Web.Domain.Entities;

public class ActivityAttachment : BaseEntity
{
    public Guid AtividadeId { get; set; }
    public Activity? Atividade { get; set; }
    public Guid ArquivoId { get; set; }
    public StoredFile? Arquivo { get; set; }
}
