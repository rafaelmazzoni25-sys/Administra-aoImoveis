using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Models;

public class OwnerPortalViewModel
{
    public string Nome { get; set; } = string.Empty;
    public IReadOnlyCollection<Property> Imoveis { get; set; } = Array.Empty<Property>();
    public IReadOnlyCollection<Inspection> Vistorias { get; set; } = Array.Empty<Inspection>();
    public IReadOnlyCollection<MaintenanceOrder> Manutencoes { get; set; } = Array.Empty<MaintenanceOrder>();
}
