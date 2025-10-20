using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.ValueObjects;

namespace AdministraAoImoveis.Domain.Repositories;

public interface IAgendaRepository
{
    Task<AgendaEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AgendaEvent agendaEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgendaEvent agendaEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgendaEvent>> GetByPropertyAndRangeAsync(Guid propertyId, TimeRange range, CancellationToken cancellationToken = default);
}
