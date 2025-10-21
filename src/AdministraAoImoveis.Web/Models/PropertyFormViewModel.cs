using System;
using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class PropertyFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Código interno")]
    public string CodigoInterno { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Endereço")]
    public string Endereco { get; set; } = string.Empty;

    [Required]
    public string Bairro { get; set; } = string.Empty;

    [Required]
    public string Cidade { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = string.Empty;

    [Required]
    public string Tipo { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Area { get; set; }

    [Range(0, int.MaxValue)]
    public int Quartos { get; set; }

    [Range(0, int.MaxValue)]
    public int Banheiros { get; set; }

    [Range(0, int.MaxValue)]
    public int Vagas { get; set; }

    [Display(Name = "Características (JSON)")]
    public string CaracteristicasJson { get; set; } = "{}";

    [Required]
    [Display(Name = "Proprietário")]
    public Guid ProprietarioId { get; set; }

    [Display(Name = "Status de disponibilidade")]
    public AvailabilityStatus StatusDisponibilidade { get; set; } = AvailabilityStatus.Disponivel;

    [Display(Name = "Disponível a partir de")]
    public DateTime? DataPrevistaDisponibilidade { get; set; }

    public IReadOnlyCollection<(Guid Id, string Nome)> Proprietarios { get; set; } = Array.Empty<(Guid, string)>();
}
