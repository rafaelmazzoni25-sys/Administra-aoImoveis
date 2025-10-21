using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class PropertyDocument : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Property? Imovel { get; set; }
    public Guid ArquivoId { get; set; }
    public StoredFile? Arquivo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Versao { get; set; } = 1;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pendente;
    public DateTime? ValidoAte { get; set; }
    public bool RequerAceiteProprietario { get; set; }
    public DateTime? RevisadoEm { get; set; }
    public string? RevisadoPor { get; set; }
    public string? Observacoes { get; set; }
    public ICollection<PropertyDocumentAcceptance> Aceites { get; set; } = new List<PropertyDocumentAcceptance>();
}
