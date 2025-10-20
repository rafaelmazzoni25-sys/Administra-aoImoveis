namespace AdministraAoImoveis.Web.Domain.Entities;

public class NegotiationEvent : BaseEntity
{
    public Guid NegociacaoId { get; set; }
    public Negotiation? Negociacao { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime OcorridoEm { get; set; } = DateTime.UtcNow;
}
