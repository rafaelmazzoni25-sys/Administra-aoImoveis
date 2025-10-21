using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AdministraAoImoveis.Web.Models;

public class InspectionDocumentViewModel
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class InspectionDocumentUploadInputModel
{
    [Required(ErrorMessage = "Informe o tipo do documento.")]
    [StringLength(80, ErrorMessage = "Tipo deve ter até 80 caracteres.")]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione um arquivo.")]
    public IFormFile? Arquivo { get; set; }
}
