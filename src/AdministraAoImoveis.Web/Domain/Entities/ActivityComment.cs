namespace AdministraAoImoveis.Web.Domain.Entities;

public class ActivityComment : BaseEntity
{
    public Guid AtividadeId { get; set; }
    public Activity? Atividade { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public DateTime ComentadoEm { get; set; } = DateTime.UtcNow;
}
