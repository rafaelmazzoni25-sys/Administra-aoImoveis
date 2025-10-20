using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class NegotiationAppService
{
    private readonly NegotiationPipelineService _pipelineService;
    private readonly INegotiationRepository _negotiationRepository;

    public NegotiationAppService(NegotiationPipelineService pipelineService, INegotiationRepository negotiationRepository)
    {
        _pipelineService = pipelineService;
        _negotiationRepository = negotiationRepository;
    }

    public async Task<NegotiationDto> CreateAsync(Guid propertyId, string interestedName, string interestedEmail, string brokerName, CancellationToken cancellationToken = default)
    {
        var negotiation = new Negotiation(Guid.NewGuid(), propertyId, interestedName, interestedEmail, brokerName, NegotiationStage.LeadCaptado, DateTime.UtcNow);
        await _pipelineService.CreateAsync(negotiation, cancellationToken);
        return negotiation.ToDto();
    }

    public async Task<NegotiationDto> AdvanceAsync(Guid negotiationId, NegotiationStage nextStage, string notes, CancellationToken cancellationToken = default)
    {
        var negotiation = await _pipelineService.AdvanceAsync(negotiationId, nextStage, notes, cancellationToken);
        return negotiation.ToDto();
    }

    public async Task<IReadOnlyCollection<NegotiationDto>> GetActiveByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var negotiations = await _negotiationRepository.GetActiveByPropertyAsync(propertyId, cancellationToken);
        return negotiations.Select(n => n.ToDto()).ToList();
    }
}
