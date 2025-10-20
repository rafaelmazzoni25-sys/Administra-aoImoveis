namespace AdministraAoImoveis.Web.Domain.Entities;

public class InAppNotification : BaseEntity
{
    public string UsuarioId { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public bool Lida { get; set; }
    public DateTime? LidaEm { get; set; }
    public string? LinkDestino { get; set; }
}
