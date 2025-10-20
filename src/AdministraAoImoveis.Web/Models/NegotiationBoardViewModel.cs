using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class NegotiationBoardViewModel
{
    public IDictionary<NegotiationStage, IReadOnlyCollection<Negotiation>> Colunas { get; set; } = new Dictionary<NegotiationStage, IReadOnlyCollection<Negotiation>>();
}
