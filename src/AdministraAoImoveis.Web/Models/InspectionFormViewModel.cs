using System;
using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class InspectionFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Imóvel")]
    public Guid ImovelId { get; set; }

    [Required]
    [Display(Name = "Tipo de vistoria")]
    public InspectionType Tipo { get; set; }

    [Required]
    [Display(Name = "Data e hora")]
    public DateTime AgendadaPara { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Responsável")]
    public string Responsavel { get; set; } = string.Empty;

    [Display(Name = "Checklist (JSON)")]
    public string ChecklistJson { get; set; } = "{}";

    [Display(Name = "Observações")]
    public string Observacoes { get; set; } = string.Empty;

    public IReadOnlyCollection<(Guid Id, string Codigo)> Imoveis { get; set; } = Array.Empty<(Guid, string)>();

    public bool PodeEditar { get; set; }

    public InspectionStatus Status { get; set; } = InspectionStatus.Agendada;

    public DateTime? Inicio { get; set; }

    public DateTime? Fim { get; set; }

    public string PendenciasTexto { get; set; } = string.Empty;
}
