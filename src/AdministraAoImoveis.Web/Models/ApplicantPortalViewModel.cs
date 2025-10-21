using System.ComponentModel.DataAnnotations;
using System.Linq;
using AdministraAoImoveis.Web.Domain.Enumerations;
using Microsoft.AspNetCore.Http;

namespace AdministraAoImoveis.Web.Models;

public class ApplicantPortalViewModel
{
    public string Nome { get; set; } = string.Empty;
    public IReadOnlyCollection<ApplicantNegotiationViewModel> Negociacoes { get; set; } = Array.Empty<ApplicantNegotiationViewModel>();
    public IReadOnlyCollection<PortalMessageViewModel> MensagensRecentes { get; set; } = Array.Empty<PortalMessageViewModel>();
    public ApplicantPortalMessageInputModel NovaMensagem { get; set; } = new();
    public ApplicantDocumentUploadInputModel Upload { get; set; } = new();
    public ApplicantVisitScheduleInputModel AgendamentoVisita { get; set; } = new();
    public bool PodeEnviarMensagem => Negociacoes.Any();
    public bool PodeEnviarDocumentos => Negociacoes.Any();
    public bool PodeAgendarVisita => Negociacoes.Any(n => n.Ativa);
}

public class ApplicantNegotiationViewModel
{
    public Guid Id { get; set; }
    public string Imovel { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public NegotiationStage Etapa { get; set; }
    public bool Ativa { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? ReservadoAte { get; set; }
    public decimal? ValorSinal { get; set; }
    public IReadOnlyCollection<PortalTimelineEntryViewModel> Timeline { get; set; } = Array.Empty<PortalTimelineEntryViewModel>();
    public IReadOnlyCollection<ApplicantFinancialSummaryViewModel> Lancamentos { get; set; } = Array.Empty<ApplicantFinancialSummaryViewModel>();
    public IReadOnlyCollection<ApplicantDocumentViewModel> Documentos { get; set; } = Array.Empty<ApplicantDocumentViewModel>();
}

public class PortalTimelineEntryViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime OcorridoEm { get; set; }
    public string Responsavel { get; set; } = string.Empty;
}

public class ApplicantFinancialSummaryViewModel
{
    public Guid Id { get; set; }
    public string TipoLancamento { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public FinancialStatus Status { get; set; }
    public DateTime? DataPrevista { get; set; }
    public DateTime? DataEfetivacao { get; set; }
}

public class ApplicantDocumentViewModel
{
    public Guid Id { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public int Versao { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
}

public class ApplicantPortalMessageInputModel
{
    [Required(ErrorMessage = "Selecione a negociação para contextualizar a mensagem.")]
    public Guid? NegociacaoId { get; set; }

    [Required(ErrorMessage = "Informe a mensagem que deseja enviar.")]
    [StringLength(2000, ErrorMessage = "A mensagem deve ter até {1} caracteres.")]
    public string Mensagem { get; set; } = string.Empty;
}

public class ApplicantDocumentUploadInputModel
{
    [Required(ErrorMessage = "Selecione a negociação.")]
    public Guid? NegociacaoId { get; set; }

    [Required(ErrorMessage = "Informe o tipo de documento.")]
    [StringLength(128)]
    public string Categoria { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione o arquivo a ser enviado.")]
    public IFormFile? Arquivo { get; set; }
}

public class ApplicantVisitScheduleInputModel
{
    [Required(ErrorMessage = "Selecione a negociação desejada.")]
    public Guid? NegociacaoId { get; set; }

    [Required(ErrorMessage = "Informe a data e hora da visita.")]
    [DataType(DataType.DateTime)]
    public DateTime? DataHora { get; set; }

    [StringLength(512)]
    public string? Observacoes { get; set; }
}
