using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AdministraAoImoveis.Web.Models;

public class MaintenanceOrderListViewModel
{
    public MaintenanceOrderStatus? Status { get; set; }
    public Guid? ImovelId { get; set; }
    [DataType(DataType.Date)]
    public DateTime? CriadaDe { get; set; }
    [DataType(DataType.Date)]
    public DateTime? CriadaAte { get; set; }
    public IReadOnlyCollection<SelectListItem> Imoveis { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<MaintenanceOrderListItemViewModel> Itens { get; set; } = Array.Empty<MaintenanceOrderListItemViewModel>();
    public IReadOnlyDictionary<MaintenanceOrderStatus, int> TotaisPorStatus { get; set; } = new Dictionary<MaintenanceOrderStatus, int>();
    public int TotalEmExecucao { get; set; }
    public int TotalAtrasadas { get; set; }
    public int Total { get; set; }
}

public class MaintenanceOrderListItemViewModel
{
    public Guid Id { get; set; }
    public string ImovelCodigo { get; set; } = string.Empty;
    public string ImovelTitulo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public MaintenanceOrderStatus Status { get; set; }
    public decimal? CustoEstimado { get; set; }
    public decimal? CustoReal { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? PrevisaoConclusao { get; set; }
    public DateTime? DataConclusao { get; set; }
    public bool EstaAtrasada { get; set; }
    public bool EmExecucao { get; set; }
}

public class MaintenanceOrderDetailViewModel
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public MaintenanceOrderStatus Status { get; set; }
    public decimal? CustoEstimado { get; set; }
    public decimal? CustoReal { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? IniciadaEm { get; set; }
    public DateTime? PrevisaoConclusao { get; set; }
    public DateTime? DataConclusao { get; set; }
    public string Responsavel { get; set; } = string.Empty;
    public string Contato { get; set; } = string.Empty;
    public Guid ImovelId { get; set; }
    public string ImovelCodigo { get; set; } = string.Empty;
    public string ImovelTitulo { get; set; } = string.Empty;
    public Guid? VistoriaId { get; set; }
    public string? VistoriaDescricao { get; set; }
    public IReadOnlyCollection<MaintenanceOrderDocumentViewModel> Documentos { get; set; } = Array.Empty<MaintenanceOrderDocumentViewModel>();
    public IReadOnlyCollection<MaintenanceOrderTimelineItemViewModel> Timeline { get; set; } = Array.Empty<MaintenanceOrderTimelineItemViewModel>();
    public MaintenanceOrderUpdateInputModel Atualizacao { get; set; } = new();
    public MaintenanceOrderAttachmentInputModel NovoDocumento { get; set; } = new();
}

public class MaintenanceOrderDocumentViewModel
{
    public Guid Id { get; set; }
    public Guid ArquivoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Tamanho { get; set; }
    public DateTime UploadEm { get; set; }
}

public class MaintenanceOrderTimelineItemViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public DateTime OcorreuEm { get; set; }
    public string Usuario { get; set; } = string.Empty;
}

public class MaintenanceOrderUpdateInputModel
{
    [Display(Name = "Status")]
    public MaintenanceOrderStatus Status { get; set; }

    [Display(Name = "Custo estimado")]
    [DataType(DataType.Currency)]
    public decimal? CustoEstimado { get; set; }

    [Display(Name = "Custo real")]
    [DataType(DataType.Currency)]
    public decimal? CustoReal { get; set; }

    [Display(Name = "Responsável")]
    [StringLength(120)]
    public string? Responsavel { get; set; }

    [Display(Name = "Contato do responsável")]
    [StringLength(120)]
    public string? Contato { get; set; }

    [Display(Name = "Categoria")]
    [StringLength(80)]
    public string? Categoria { get; set; }

    [Display(Name = "Previsão de conclusão")]
    [DataType(DataType.Date)]
    public DateTime? PrevisaoConclusao { get; set; }

    [Display(Name = "Observações")]
    [StringLength(500)]
    public string? Observacoes { get; set; }
}

public class MaintenanceOrderAttachmentInputModel
{
    [Required]
    [Display(Name = "Arquivo")]
    public IFormFile? Arquivo { get; set; }

    [Display(Name = "Categoria")]
    [StringLength(80)]
    public string? Categoria { get; set; }
}

public class MaintenanceOrderCreateViewModel
{
    public MaintenanceOrderCreateInputModel Ordem { get; set; } = new();
    public IReadOnlyCollection<SelectListItem> Imoveis { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> Vistorias { get; set; } = Array.Empty<SelectListItem>();
}

public class MaintenanceOrderCreateInputModel
{
    [Required]
    [Display(Name = "Imóvel")]
    public Guid? ImovelId { get; set; }

    [Required]
    [StringLength(180)]
    [Display(Name = "Título")]
    public string? Titulo { get; set; }

    [Required]
    [StringLength(1000)]
    [Display(Name = "Descrição detalhada")]
    public string? Descricao { get; set; }

    [StringLength(80)]
    [Display(Name = "Categoria")]
    public string? Categoria { get; set; }

    [Display(Name = "Responsável")]
    [StringLength(120)]
    public string? Responsavel { get; set; }

    [Display(Name = "Contato do responsável")]
    [StringLength(120)]
    public string? Contato { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Custo estimado")]
    public decimal? CustoEstimado { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Previsão de conclusão")]
    public DateTime? PrevisaoConclusao { get; set; }

    [Display(Name = "Vincular vistoria")]
    public Guid? VistoriaId { get; set; }
}
