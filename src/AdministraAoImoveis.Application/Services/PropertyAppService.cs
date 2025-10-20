using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class PropertyAppService
{
    private readonly PropertyAvailabilityService _availabilityService;
    private readonly IPropertyRepository _propertyRepository;

    public PropertyAppService(PropertyAvailabilityService availabilityService, IPropertyRepository propertyRepository)
    {
        _availabilityService = availabilityService;
        _propertyRepository = propertyRepository;
    }

    public async Task<PropertyDto> RegisterAsync(string code, string address, string type, decimal sizeSquareMeters, int bedrooms, string ownerName, string? description, CancellationToken cancellationToken = default)
    {
        var property = new Property(Guid.NewGuid(), code, address, type, sizeSquareMeters, bedrooms, ownerName, PropertyOperationalStatus.Disponivel, DateTime.UtcNow, description);
        await _availabilityService.RegisterPropertyAsync(property, cancellationToken);
        return property.ToDto();
    }

    public async Task<PropertyDto> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetByCodeAsync(code, cancellationToken) ?? throw new InvalidOperationException("Imóvel não encontrado");
        return property.ToDto();
    }

    public async Task<IReadOnlyCollection<PropertyDto>> SearchAvailableAsync(DateTime reference, CancellationToken cancellationToken = default)
    {
        var properties = await _propertyRepository.SearchAvailableAsync(reference, cancellationToken);
        return properties.Select(p => p.ToDto()).ToList();
    }
}
