using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class NegotiationPipelineService
{
    private readonly INegotiationRepository _negotiationRepository;
    private readonly IPropertyRepository _propertyRepository;

    public NegotiationPipelineService(INegotiationRepository negotiationRepository, IPropertyRepository propertyRepository)
    {
        _negotiationRepository = negotiationRepository;
        _propertyRepository = propertyRepository;
    }

    public async Task<Negotiation> CreateAsync(Negotiation negotiation, CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetByIdAsync(negotiation.PropertyId, cancellationToken) ?? throw new InvalidOperationException("Imóvel não encontrado");
        if (property.ActiveNegotiationId.HasValue)
        {
            throw new InvalidOperationException("O imóvel já possui negociação em andamento.");
        }

        await _negotiationRepository.AddAsync(negotiation, cancellationToken);
        property.AttachNegotiation(negotiation.Id);
        await _propertyRepository.UpdateAsync(property, cancellationToken);
        return negotiation;
    }

    public async Task<Negotiation> AdvanceAsync(Guid negotiationId, NegotiationStage nextStage, string reason, CancellationToken cancellationToken = default)
    {
        var negotiation = await EnsureNegotiationAsync(negotiationId, cancellationToken);
        negotiation.AdvanceTo(nextStage, reason);
        await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);

        if (nextStage is NegotiationStage.Cancelada or NegotiationStage.Concluida)
        {
            var property = await _propertyRepository.GetByIdAsync(negotiation.PropertyId, cancellationToken);
            if (property is not null)
            {
                property.ReleaseNegotiation(negotiation.Id);
                await _propertyRepository.UpdateAsync(property, cancellationToken);
            }
        }

        return negotiation;
    }

    public async Task<IReadOnlyList<Negotiation>> CheckExpiredProposalsAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _negotiationRepository.GetExpiringProposalsAsync(DateTime.UtcNow, cancellationToken);
        foreach (var negotiation in expired.Where(n => n.IsExpired(DateTime.UtcNow)))
        {
            negotiation.AdvanceTo(NegotiationStage.Cancelada, "Proposta expirada automaticamente");
            await _negotiationRepository.UpdateAsync(negotiation, cancellationToken);
            var property = await _propertyRepository.GetByIdAsync(negotiation.PropertyId, cancellationToken);
            if (property is not null)
            {
                property.ReleaseNegotiation(negotiation.Id);
                await _propertyRepository.UpdateAsync(property, cancellationToken);
            }
        }

        return expired;
    }

    private async Task<Negotiation> EnsureNegotiationAsync(Guid negotiationId, CancellationToken cancellationToken)
    {
        return await _negotiationRepository.GetByIdAsync(negotiationId, cancellationToken) ?? throw new InvalidOperationException("Negociação não encontrada");
    }
}
