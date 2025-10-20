using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class Activity : BaseEntity
{
    public ActivityType Tipo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public ActivityLinkType VinculoTipo { get; set; }
    public Guid VinculoId { get; set; }
    public string Setor { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public PriorityLevel Prioridade { get; set; } = PriorityLevel.Media;
    public DateTime? DataLimite { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Aberta;
    public ICollection<ActivityComment> Comentarios { get; set; } = new List<ActivityComment>();
    public ICollection<ActivityAttachment> Anexos { get; set; } = new List<ActivityAttachment>();
}
