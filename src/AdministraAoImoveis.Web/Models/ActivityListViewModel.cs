using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class ActivityListViewModel
{
    public ActivityStatus? Status { get; init; }
    public PriorityLevel? Prioridade { get; init; }
    public string? Responsavel { get; init; }
    public IReadOnlyCollection<ActivityListItemViewModel> Itens { get; init; } = Array.Empty<ActivityListItemViewModel>();
    public IReadOnlyDictionary<ActivityStatus, int> TotaisPorStatus { get; init; } =
        new Dictionary<ActivityStatus, int>();
    public int Total { get; init; }
    public int TotalAtrasadas { get; init; }
    public int TotalEmRisco { get; init; }
}

public class ActivityListItemViewModel
{
    public Activity Activity { get; init; } = null!;
    public bool EstaAtrasada { get; init; }
    public bool EmRisco { get; init; }
    public TimeSpan? TempoRestante { get; init; }
    public double PercentualSlaConsumido { get; init; }
    public int Comentarios { get; init; }
    public int Anexos { get; init; }
}
