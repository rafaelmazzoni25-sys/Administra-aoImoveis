using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class PropertyDocumentAcceptance : BaseEntity
{
    public Guid DocumentoId { get; set; }
    public PropertyDocument? Documento { get; set; }
    public DocumentAcceptanceType Tipo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string UsuarioSistema { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
}
