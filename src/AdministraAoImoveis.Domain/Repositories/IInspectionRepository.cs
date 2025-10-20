using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface IInspectionRepository
{
    Task<Inspection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inspection>> GetScheduledForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task AddAsync(Inspection inspection, CancellationToken cancellationToken = default);
    Task UpdateAsync(Inspection inspection, CancellationToken cancellationToken = default);
}
