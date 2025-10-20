using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class ContextMessage : BaseEntity
{
    public ActivityLinkType ContextoTipo { get; set; }
    public Guid ContextoId { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public DateTime EnviadaEm { get; set; } = DateTime.UtcNow;
    public ICollection<ContextMessageMention> Mentions { get; set; } = new List<ContextMessageMention>();
}
