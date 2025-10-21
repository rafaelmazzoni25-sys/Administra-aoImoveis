using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class PropertyDetailViewModel
{
    public Guid Id { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public int Quartos { get; set; }
    public int Banheiros { get; set; }
    public int Vagas { get; set; }
    public AvailabilityStatus StatusDisponibilidade { get; set; }
    public DateTime? DataPrevistaDisponibilidade { get; set; }
    public string CaracteristicasJson { get; set; } = string.Empty;
    public Owner? Proprietario { get; set; }
    public IReadOnlyCollection<Negotiation> Negociacoes { get; set; } = Array.Empty<Negotiation>();
    public IReadOnlyCollection<Inspection> Vistorias { get; set; } = Array.Empty<Inspection>();
    public IReadOnlyCollection<Activity> Atividades { get; set; } = Array.Empty<Activity>();
    public IReadOnlyCollection<PropertyHistoryEvent> Historico { get; set; } = Array.Empty<PropertyHistoryEvent>();
    public IReadOnlyCollection<PropertyDocumentSummaryViewModel> Documentos { get; set; } = Array.Empty<PropertyDocumentSummaryViewModel>();
    public IReadOnlyCollection<PropertyMaintenanceSummaryViewModel> Manutencoes { get; set; } = Array.Empty<PropertyMaintenanceSummaryViewModel>();
    public ContractSummaryViewModel? ContratoAtivo { get; set; }
    public IReadOnlyCollection<ContractSummaryViewModel> Contratos { get; set; } = Array.Empty<ContractSummaryViewModel>();
}

public class PropertyMaintenanceSummaryViewModel
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public MaintenanceOrderStatus Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? PrevisaoConclusao { get; set; }
    public bool EmExecucao { get; set; }
}
