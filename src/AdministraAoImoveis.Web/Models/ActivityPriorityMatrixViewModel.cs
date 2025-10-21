using System;
using System.Collections.Generic;
using AdministraAoImoveis.Web.Domain.Entities;
using AdministraAoImoveis.Web.Domain.Enumerations;

namespace AdministraAoImoveis.Web.Models;

public class ActivityPriorityMatrixViewModel
{
    public IReadOnlyCollection<string> Responsaveis { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<PriorityLevel, IReadOnlyCollection<Activity>> PorPrioridade { get; init; }
        = new Dictionary<PriorityLevel, IReadOnlyCollection<Activity>>();

    public DateTime AtualizadoEm { get; init; } = DateTime.UtcNow;
}
