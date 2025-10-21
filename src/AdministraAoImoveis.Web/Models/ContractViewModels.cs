using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Enumerations;
using Microsoft.AspNetCore.Http;

namespace AdministraAoImoveis.Web.Models;

public class ContractListViewModel
{
    public bool IncluirEncerrados { get; set; }
    public IReadOnlyCollection<ContractSummaryViewModel> Contratos { get; set; } = Array.Empty<ContractSummaryViewModel>();
}

public class ContractSummaryViewModel
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyTitle { get; set; } = string.Empty;
    public string Interessado { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public decimal ValorAluguel { get; set; }
    public decimal Encargos { get; set; }
}

public class ContractGenerationViewModel
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyTitle { get; set; } = string.Empty;
    public IReadOnlyCollection<NegotiationOptionViewModel> Negotiations { get; set; } = Array.Empty<NegotiationOptionViewModel>();
    public ContractGenerationInputModel Input { get; set; } = new();
}

public class NegotiationOptionViewModel
{
    public Guid Id { get; set; }
    public string Interessado { get; set; } = string.Empty;
    public NegotiationStage Etapa { get; set; }
    public decimal? ValorProposta { get; set; }
    public DateTime CriadaEm { get; set; }
}

public class ContractGenerationInputModel
{
    [Required]
    public Guid PropertyId { get; set; }

    [Required(ErrorMessage = "Selecione uma negociação.")]
    public Guid NegotiationId { get; set; }

    [Required(ErrorMessage = "Informe a data de início da vigência.")]
    [DataType(DataType.Date)]
    public DateTime DataInicio { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DataFim { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Informe um valor válido para o aluguel.")]
    public decimal ValorAluguel { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Informe um valor válido para os encargos.")]
    public decimal Encargos { get; set; }
}

public class ContractDetailViewModel
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string PropertyTitle { get; set; } = string.Empty;
    public string Interessado { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public decimal ValorAluguel { get; set; }
    public decimal Encargos { get; set; }
    public IReadOnlyCollection<ContractDocumentVersionViewModel> Documentos { get; set; } = Array.Empty<ContractDocumentVersionViewModel>();
    public ContractAttachmentInputModel Anexo { get; set; } = new();
    public ContractClosureInputModel Encerramento { get; set; } = new();
}

public class ContractDocumentVersionViewModel
{
    public Guid DocumentoId { get; set; }
    public Guid ArquivoId { get; set; }
    public int Versao { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ValidoAte { get; set; }
    public bool Atual { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
}

public class ContractAttachmentInputModel
{
    [Required]
    public Guid ContractId { get; set; }

    [Required(ErrorMessage = "Selecione um arquivo para anexar.")]
    public IFormFile? Arquivo { get; set; }
}

public class ContractClosureInputModel
{
    [Required]
    public Guid ContractId { get; set; }

    [Required(ErrorMessage = "Informe a data de encerramento.")]
    [DataType(DataType.Date)]
    public DateTime DataEncerramento { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }
}
