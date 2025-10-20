namespace AdministraAoImoveis.Web.Domain.Entities;

public class InterestedParty : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string? UsuarioId { get; set; }
    public ICollection<Negotiation> Negociacoes { get; set; } = new List<Negotiation>();
}
