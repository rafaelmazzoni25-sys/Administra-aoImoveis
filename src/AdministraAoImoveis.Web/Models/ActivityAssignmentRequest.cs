using System.ComponentModel.DataAnnotations;

namespace AdministraAoImoveis.Web.Models;

public class ActivityAssignmentRequest
{
    [Required]
    public Guid Id { get; set; }

    public string? NovoResponsavel { get; set; }
}
