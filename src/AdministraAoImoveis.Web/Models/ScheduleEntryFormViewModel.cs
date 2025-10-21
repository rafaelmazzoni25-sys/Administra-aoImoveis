using System;
using System.ComponentModel.DataAnnotations;

namespace AdministraAoImoveis.Web.Models;

public class ScheduleEntryFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Display(Name = "Setor")]
    public string? Setor { get; set; }

    [Required]
    [Display(Name = "Início")]
    public DateTime Inicio { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Fim")]
    public DateTime Fim { get; set; } = DateTime.UtcNow.AddHours(1);

    [Display(Name = "Responsável")]
    public string? Responsavel { get; set; }

    [Display(Name = "Imóvel")]
    public Guid? ImovelId { get; set; }

    [Display(Name = "Vistoria vinculada")]
    public Guid? VistoriaId { get; set; }

    [Display(Name = "Negociação vinculada")]
    public Guid? NegociacaoId { get; set; }

    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }

    public IReadOnlyCollection<(Guid Id, string Codigo)> Imoveis { get; set; } = Array.Empty<(Guid, string)>();
    public IReadOnlyCollection<(Guid Id, string Nome)> Negociacoes { get; set; } = Array.Empty<(Guid, string)>();
    public IReadOnlyCollection<(Guid Id, string Descricao)> Vistorias { get; set; } = Array.Empty<(Guid, string)>();
}
