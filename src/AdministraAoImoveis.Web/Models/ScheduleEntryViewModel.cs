using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Models;

public class ScheduleEntryViewModel
{
    public Guid Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Responsavel { get; init; } = string.Empty;
    public DateTime Inicio { get; init; }
    public DateTime Fim { get; init; }
    public Guid? ImovelId { get; init; }
    public Guid? VistoriaId { get; init; }
    public Guid? NegociacaoId { get; init; }
    public string Observacoes { get; init; } = string.Empty;
    public IReadOnlyCollection<ScheduleEntryConflictViewModel> Conflitos { get; init; } = Array.Empty<ScheduleEntryConflictViewModel>();

    public bool TemConflito => Conflitos.Count > 0;

    public static ScheduleEntryViewModel FromEntity(ScheduleEntry entry, IReadOnlyCollection<ScheduleEntryConflictViewModel> conflitos)
    {
        return new ScheduleEntryViewModel
        {
            Id = entry.Id,
            Titulo = entry.Titulo,
            Tipo = entry.Tipo,
            Responsavel = entry.Responsavel,
            Inicio = entry.Inicio,
            Fim = entry.Fim,
            ImovelId = entry.ImovelId,
            VistoriaId = entry.VistoriaId,
            NegociacaoId = entry.NegociacaoId,
            Observacoes = entry.Observacoes,
            Conflitos = conflitos
        };
    }
}

public class ScheduleEntryConflictViewModel
{
    public Guid Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string Responsavel { get; init; } = string.Empty;
    public DateTime Inicio { get; init; }
    public DateTime Fim { get; init; }
    public string Escopo { get; init; } = string.Empty;
}
