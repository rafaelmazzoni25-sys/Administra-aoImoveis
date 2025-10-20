using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.ValueObjects;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryAgendaRepository : IAgendaRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryAgendaRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(AgendaEvent agendaEvent, CancellationToken cancellationToken = default)
    {
        _store.AgendaEvents[agendaEvent.Id] = agendaEvent;
        return Task.CompletedTask;
    }

    public Task<AgendaEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.AgendaEvents.TryGetValue(id, out var agendaEvent);
        return Task.FromResult<AgendaEvent?>(agendaEvent);
    }

    public Task<IReadOnlyList<AgendaEvent>> GetByPropertyAndRangeAsync(Guid propertyId, TimeRange range, CancellationToken cancellationToken = default)
    {
        var result = _store.AgendaEvents.Values
            .Where(e => e.PropertyId == propertyId && e.TimeRange.Overlaps(range))
            .ToList();
        return Task.FromResult<IReadOnlyList<AgendaEvent>>(result);
    }

    public Task UpdateAsync(AgendaEvent agendaEvent, CancellationToken cancellationToken = default)
    {
        _store.AgendaEvents[agendaEvent.Id] = agendaEvent;
        return Task.CompletedTask;
    }
}
