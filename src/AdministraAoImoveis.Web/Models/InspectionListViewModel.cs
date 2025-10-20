using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class InspectionListViewModel
{
    public InspectionStatus? Status { get; set; }
    public InspectionType? Tipo { get; set; }
    public IReadOnlyCollection<Inspection> Vistorias { get; set; } = Array.Empty<Inspection>();
}
