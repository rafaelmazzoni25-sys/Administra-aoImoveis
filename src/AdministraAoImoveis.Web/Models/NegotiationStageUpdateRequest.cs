using System.ComponentModel.DataAnnotations;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class NegotiationStageUpdateRequest
{
    [Required]
    public Guid NegotiationId { get; set; }

    [Required]
    public NegotiationStage Stage { get; set; }

    public decimal? ValorSinal { get; set; }

    public DateTime? ReservadoAte { get; set; }
}
