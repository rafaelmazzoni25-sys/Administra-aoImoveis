using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Models;

public class ScheduleCalendarViewModel
{
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string ModoVisualizacao { get; set; } = AgendaViewMode.Semana;
    public string? TipoSelecionado { get; set; }
    public string? ResponsavelSelecionado { get; set; }
    public string? SetorSelecionado { get; set; }
    public IReadOnlyCollection<string> TiposDisponiveis { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ResponsaveisDisponiveis { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> SetoresDisponiveis { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<ScheduleEntryViewModel> Compromissos { get; set; } = Array.Empty<ScheduleEntryViewModel>();
}

public static class AgendaViewMode
{
    public const string Semana = "semana";
    public const string Mes = "mes";
    public const string Personalizado = "personalizado";

    public static bool IsValid(string? value)
    {
        return value is Semana or Mes or Personalizado;
    }
}
