using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class PropertyListViewModel
{
    public AvailabilityStatus? Status { get; set; }
    public string? Cidade { get; set; }
    public string? Bairro { get; set; }
    public DateTime? DisponivelAte { get; set; }
    public IReadOnlyCollection<PropertySummaryViewModel> Resultados { get; set; } = Array.Empty<PropertySummaryViewModel>();
}

public class PropertySummaryViewModel
{
    public Guid Id { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public AvailabilityStatus Status { get; set; }
    public DateTime? DataPrevistaDisponibilidade { get; set; }
}
