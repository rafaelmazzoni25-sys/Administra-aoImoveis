namespace AdministraAoImoveis.Web.Domain.Entities;

public class AuditLogEntry : BaseEntity
{
    public string Entidade { get; set; } = string.Empty;
    public Guid EntidadeId { get; set; }
    public string Operacao { get; set; } = string.Empty;
    public string Antes { get; set; } = string.Empty;
    public string Depois { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
}
