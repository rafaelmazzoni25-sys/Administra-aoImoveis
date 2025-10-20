using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class OwnerPortalViewModel
{
    public string Nome { get; set; } = string.Empty;
    public OwnerPortalMetricsViewModel Metricas { get; set; } = new();
    public IReadOnlyCollection<OwnerPortalPropertyViewModel> Imoveis { get; set; } = Array.Empty<OwnerPortalPropertyViewModel>();
    public IReadOnlyCollection<OwnerDocumentSummaryViewModel> DocumentosPendentes { get; set; } = Array.Empty<OwnerDocumentSummaryViewModel>();
    public IReadOnlyCollection<PortalMessageViewModel> MensagensRecentes { get; set; } = Array.Empty<PortalMessageViewModel>();
    public OwnerPortalMessageInputModel NovaMensagem { get; set; } = new();
    public bool PodeEnviarMensagem { get; set; }
}

public class OwnerPortalMetricsViewModel
{
    public int TotalImoveis { get; set; }
    public int Disponiveis { get; set; }
    public int EmNegociacao { get; set; }
    public int EmManutencao { get; set; }
    public int VistoriasPendentes { get; set; }
    public int ManutencoesAbertas { get; set; }
    public int PendenciasCriticas { get; set; }
    public int DocumentosPendentes { get; set; }
}

public class OwnerPortalPropertyViewModel
{
    public Guid Id { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public AvailabilityStatus Status { get; set; }
    public DateTime? DisponivelEm { get; set; }
    public int PendenciasCriticas { get; set; }
    public IReadOnlyCollection<OwnerPortalInspectionSummary> ProximasVistorias { get; set; } = Array.Empty<OwnerPortalInspectionSummary>();
    public IReadOnlyCollection<OwnerPortalMaintenanceSummary> ManutencoesEmAberto { get; set; } = Array.Empty<OwnerPortalMaintenanceSummary>();
    public IReadOnlyCollection<OwnerPortalNegotiationSummary> NegociacoesAtivas { get; set; } = Array.Empty<OwnerPortalNegotiationSummary>();
}

public class OwnerPortalInspectionSummary
{
    public Guid Id { get; set; }
    public InspectionStatus Status { get; set; }
    public InspectionType Tipo { get; set; }
    public DateTime AgendadaPara { get; set; }
    public string Responsavel { get; set; } = string.Empty;
}

public class OwnerPortalMaintenanceSummary
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public MaintenanceOrderStatus Status { get; set; }
    public DateTime? PrevisaoConclusao { get; set; }
    public string Responsavel { get; set; } = string.Empty;
}

public class OwnerPortalNegotiationSummary
{
    public Guid Id { get; set; }
    public NegotiationStage Etapa { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? ReservadoAte { get; set; }
    public decimal? ValorSinal { get; set; }
    public string Interessado { get; set; } = string.Empty;
    public decimal TotalPrevisto { get; set; }
    public decimal TotalRecebido { get; set; }
    public decimal TotalPendente { get; set; }
}

public class OwnerDocumentSummaryViewModel
{
    public Guid DocumentoId { get; set; }
    public Guid ImovelId { get; set; }
    public string Imovel { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int Versao { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTime? ValidoAte { get; set; }
    public bool RequerAceite { get; set; }
}

public class OwnerPortalMessageInputModel
{
    [Required(ErrorMessage = "Selecione o imóvel para contextualizar a mensagem.")]
    public Guid? ImovelId { get; set; }

    [Required(ErrorMessage = "Escreva a mensagem a ser enviada.")]
    [StringLength(2000, ErrorMessage = "A mensagem deve ter até {1} caracteres.")]
    public string Mensagem { get; set; } = string.Empty;
}

public class PortalMessageViewModel
{
    public Guid Id { get; set; }
    public string Contexto { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public DateTime EnviadaEm { get; set; }
    public string Conteudo { get; set; } = string.Empty;
}
