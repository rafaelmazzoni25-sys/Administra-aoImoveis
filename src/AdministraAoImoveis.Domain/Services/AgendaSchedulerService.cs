using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.ValueObjects;

namespace AdministraAoImoveis.Domain.Services;

public sealed class AgendaSchedulerService
{
    private readonly IAgendaRepository _agendaRepository;

    public AgendaSchedulerService(IAgendaRepository agendaRepository)
    {
        _agendaRepository = agendaRepository;
    }

    public async Task<AgendaEvent> ScheduleAsync(AgendaEvent agendaEvent, CancellationToken cancellationToken = default)
    {
        await EnsureNoConflictsAsync(agendaEvent.PropertyId, agendaEvent.TimeRange, cancellationToken);
        await _agendaRepository.AddAsync(agendaEvent, cancellationToken);
        return agendaEvent;
    }

    public async Task<AgendaEvent> RescheduleAsync(Guid agendaId, TimeRange newRange, CancellationToken cancellationToken = default)
    {
        var agenda = await EnsureAgendaAsync(agendaId, cancellationToken);
        await EnsureNoConflictsAsync(agenda.PropertyId, newRange, cancellationToken, agendaId);
        agenda.Reschedule(newRange);
        await _agendaRepository.UpdateAsync(agenda, cancellationToken);
        return agenda;
    }

    private async Task EnsureNoConflictsAsync(Guid propertyId, TimeRange range, CancellationToken cancellationToken, Guid? ignoreId = null)
    {
        var events = await _agendaRepository.GetByPropertyAndRangeAsync(propertyId, range, cancellationToken);
        if (events.Any(e => e.Id != ignoreId && e.TimeRange.Overlaps(range)))
        {
            throw new InvalidOperationException("Existe conflito de agenda para o imóvel.");
        }
    }

    private async Task<AgendaEvent> EnsureAgendaAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _agendaRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Evento de agenda não encontrado");
    }
}
