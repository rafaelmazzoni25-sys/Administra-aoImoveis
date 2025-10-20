namespace AdministraAoImoveis.Web.Domain.Entities;

public class ContextMessageMention : BaseEntity
{
    public Guid MensagemId { get; set; }
    public ContextMessage? Mensagem { get; set; }
    public string UsuarioMencionadoId { get; set; } = string.Empty;
    public bool Notificado { get; set; }
}
