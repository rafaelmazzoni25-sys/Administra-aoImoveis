using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class LeadCrmService
{
    private readonly ILeadRepository _leadRepository;

    public LeadCrmService(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<Lead> RegisterAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        await _leadRepository.AddAsync(lead, cancellationToken);
        return lead;
    }

    public async Task<Lead> AssignAsync(Guid leadId, string owner, CancellationToken cancellationToken = default)
    {
        var lead = await EnsureLeadAsync(leadId, cancellationToken);
        lead.AssignTo(owner);
        await _leadRepository.UpdateAsync(lead, cancellationToken);
        return lead;
    }

    public async Task<Lead> RecordInteractionAsync(Guid leadId, string description, string actor, CancellationToken cancellationToken = default)
    {
        var lead = await EnsureLeadAsync(leadId, cancellationToken);
        lead.RegisterInteraction(description, actor);
        await _leadRepository.UpdateAsync(lead, cancellationToken);
        return lead;
    }

    public async Task<Lead> ChangeStatusAsync(Guid leadId, LeadStatus status, string actor, CancellationToken cancellationToken = default)
    {
        var lead = await EnsureLeadAsync(leadId, cancellationToken);
        lead.ChangeStatus(status, actor);
        await _leadRepository.UpdateAsync(lead, cancellationToken);
        return lead;
    }

    private async Task<Lead> EnsureLeadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _leadRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Lead não localizado");
    }
}
