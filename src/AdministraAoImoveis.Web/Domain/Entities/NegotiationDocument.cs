namespace AdministraAoImoveis.Web.Domain.Entities;

public class NegotiationDocument : BaseEntity
{
    public Guid NegociacaoId { get; set; }
    public Negotiation? Negociacao { get; set; }
    public Guid ArquivoId { get; set; }
    public StoredFile? Arquivo { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public int Versao { get; set; } = 1;
}
