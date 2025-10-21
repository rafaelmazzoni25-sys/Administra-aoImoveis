using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Domain.Entities;

public class Property : BaseEntity
{
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
    public string CaracteristicasJson { get; set; } = "{}";
    public Guid ProprietarioId { get; set; }
    public Owner? Proprietario { get; set; }
    public AvailabilityStatus StatusDisponibilidade { get; set; } = AvailabilityStatus.Disponivel;
    public DateTime? DataPrevistaDisponibilidade { get; set; }
    public Guid? ContratoAtivoId { get; set; }
    public Contract? ContratoAtivo { get; set; }
    public ICollection<PropertyHistoryEvent> Historico { get; set; } = new List<PropertyHistoryEvent>();
    public ICollection<Negotiation> Negociacoes { get; set; } = new List<Negotiation>();
    public ICollection<Contract> Contratos { get; set; } = new List<Contract>();
    public ICollection<Inspection> Vistorias { get; set; } = new List<Inspection>();
    public ICollection<Activity> Atividades { get; set; } = new List<Activity>();
    public ICollection<MaintenanceOrder> Manutencoes { get; set; } = new List<MaintenanceOrder>();
    public ICollection<PropertyDocument> Documentos { get; set; } = new List<PropertyDocument>();
}
