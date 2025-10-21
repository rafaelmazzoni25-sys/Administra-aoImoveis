using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Enumerations;
using Microsoft.AspNetCore.Http;

namespace AdministraAoImoveis.Web.Models;

public class PropertyDocumentVersionViewModel
{
    public Guid Id { get; set; }
    public int Versao { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ValidoAte { get; set; }
    public bool Expirado { get; set; }
    public bool RequerAceiteProprietario { get; set; }
    public DateTime? RevisadoEm { get; set; }
    public string? RevisadoPor { get; set; }
    public string? Observacoes { get; set; }
    public IReadOnlyCollection<PropertyDocumentAcceptanceViewModel> Aceites { get; set; }
        = Array.Empty<PropertyDocumentAcceptanceViewModel>();
}

public class PropertyDocumentAcceptanceViewModel
{
    public Guid Id { get; set; }
    public DocumentAcceptanceType Tipo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string UsuarioSistema { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public DateTime RegistradoEm { get; set; }
}

public class PropertyDocumentGroupViewModel
{
    public string Descricao { get; set; } = string.Empty;
    public PropertyDocumentVersionViewModel? VersaoAtual { get; set; }
    public IReadOnlyCollection<PropertyDocumentVersionViewModel> Historico { get; set; }
        = Array.Empty<PropertyDocumentVersionViewModel>();
}

public class PropertyDocumentSummaryViewModel
{
    public string Descricao { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public int Versao { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ValidoAte { get; set; }
    public bool Expirado { get; set; }
}

public class PropertyDocumentUploadInputModel
{
    [Required(ErrorMessage = "Descrição é obrigatória.")]
    [StringLength(120, ErrorMessage = "Descrição deve ter até 120 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Display(Name = "Observações")]
    [StringLength(500, ErrorMessage = "Observações deve ter até 500 caracteres.")]
    public string? Observacoes { get; set; }

    [Display(Name = "Validade")]
    public DateTime? ValidoAte { get; set; }

    [Display(Name = "Solicitar aceite do proprietário")]
    public bool RequerAceiteProprietario { get; set; }

    [Display(Name = "Aprovar automaticamente após upload")]
    public bool AprovarAutomaticamente { get; set; }

    [Required(ErrorMessage = "Selecione um arquivo.")]
    public IFormFile? Arquivo { get; set; }
}

public class PropertyDocumentReviewInputModel
{
    [Display(Name = "Aprovar documento")]
    public bool Aprovar { get; set; }

    [StringLength(400, ErrorMessage = "Comentário deve ter até 400 caracteres.")]
    public string? Comentario { get; set; }
}

public class PropertyDocumentLibraryViewModel
{
    public Guid ImovelId { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public IReadOnlyCollection<PropertyDocumentGroupViewModel> Grupos { get; set; }
        = Array.Empty<PropertyDocumentGroupViewModel>();
    public PropertyDocumentUploadInputModel Upload { get; set; } = new();
    public IReadOnlyCollection<DocumentTemplateOptionViewModel> ModelosDisponiveis { get; set; }
        = Array.Empty<DocumentTemplateOptionViewModel>();
}

public class PropertyDocumentAcceptanceInputModel
{
    [Required(ErrorMessage = "Selecione o tipo de aceite.")]
    public DocumentAcceptanceType Tipo { get; set; }

    [Required(ErrorMessage = "Informe o nome do responsável.")]
    [StringLength(120, ErrorMessage = "Nome deve ter até 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(120, ErrorMessage = "Cargo deve ter até 120 caracteres.")]
    public string Cargo { get; set; } = string.Empty;
}

public class DocumentTemplateOptionViewModel
{
    public string Valor { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}
