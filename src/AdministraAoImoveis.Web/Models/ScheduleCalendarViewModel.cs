using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Models;

public class ScheduleCalendarViewModel
{
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public IReadOnlyCollection<ScheduleEntryViewModel> Compromissos { get; set; } = Array.Empty<ScheduleEntryViewModel>();
}
