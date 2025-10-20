using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Models;

public class ApplicantPortalViewModel
{
    public string Nome { get; set; } = string.Empty;
    public IReadOnlyCollection<Negotiation> Negociacoes { get; set; } = Array.Empty<Negotiation>();
}
