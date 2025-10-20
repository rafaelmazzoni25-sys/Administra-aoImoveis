using System.Linq;
using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class LeadAppService
{
    private readonly LeadCrmService _leadCrmService;
    private readonly ILeadRepository _leadRepository;

    public LeadAppService(LeadCrmService leadCrmService, ILeadRepository leadRepository)
    {
        _leadCrmService = leadCrmService;
        _leadRepository = leadRepository;
    }

    public async Task<LeadDto> RegisterAsync(string name, string contact, LeadSource source, string desiredPropertyType, decimal? budget, CancellationToken cancellationToken = default)
    {
        var lead = new Lead(Guid.NewGuid(), name, contact, source, desiredPropertyType, budget);
        await _leadCrmService.RegisterAsync(lead, cancellationToken);
        return lead.ToDto();
    }

    public async Task<LeadDto> AssignAsync(Guid leadId, string owner, CancellationToken cancellationToken = default)
    {
        var lead = await _leadCrmService.AssignAsync(leadId, owner, cancellationToken);
        return lead.ToDto();
    }

    public async Task<LeadDto> ChangeStatusAsync(Guid leadId, LeadStatus status, string actor, CancellationToken cancellationToken = default)
    {
        var lead = await _leadCrmService.ChangeStatusAsync(leadId, status, actor, cancellationToken);
        return lead.ToDto();
    }

    public async Task<LeadDto> RecordInteractionAsync(Guid leadId, string description, string actor, CancellationToken cancellationToken = default)
    {
        var lead = await _leadCrmService.RecordInteractionAsync(leadId, description, actor, cancellationToken);
        return lead.ToDto();
    }

    public async Task<IReadOnlyCollection<LeadDto>> SearchAsync(string? assignedTo, CancellationToken cancellationToken = default)
    {
        var leads = await _leadRepository.SearchAsync(assignedTo, cancellationToken);
        return leads.Select(l => l.ToDto()).ToList();
    }
}
