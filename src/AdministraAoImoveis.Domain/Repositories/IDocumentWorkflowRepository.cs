using AdministraAoImoveis.Domain.Entities;

namespace AdministraAoImoveis.Domain.Repositories;

public interface IDocumentWorkflowRepository
{
    Task AddAsync(DocumentWorkflow workflow, CancellationToken cancellationToken = default);
    Task UpdateAsync(DocumentWorkflow workflow, CancellationToken cancellationToken = default);
    Task<DocumentWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DocumentWorkflow>> GetByReferenceAsync(Guid referenceId, string referenceType, CancellationToken cancellationToken = default);
}
