using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class ActivityListViewModel
{
    public ActivityStatus? Status { get; set; }
    public PriorityLevel? Prioridade { get; set; }
    public string? Responsavel { get; set; }
    public IReadOnlyCollection<Activity> Atividades { get; set; } = Array.Empty<Activity>();
}
