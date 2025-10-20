using System.Linq;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Infrastructure.Persistence;

public sealed class InMemoryDocumentWorkflowRepository : IDocumentWorkflowRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryDocumentWorkflowRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(DocumentWorkflow workflow, CancellationToken cancellationToken = default)
    {
        _store.DocumentWorkflows[workflow.Id] = workflow;
        return Task.CompletedTask;
    }

    public Task<DocumentWorkflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.DocumentWorkflows.TryGetValue(id, out var workflow);
        return Task.FromResult<DocumentWorkflow?>(workflow);
    }

    public Task<IReadOnlyCollection<DocumentWorkflow>> GetByReferenceAsync(Guid referenceId, string referenceType, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<DocumentWorkflow> result = _store.DocumentWorkflows.Values
            .Where(w => w.ReferenceId == referenceId && string.Equals(w.ReferenceType, referenceType, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(DocumentWorkflow workflow, CancellationToken cancellationToken = default)
    {
        _store.DocumentWorkflows[workflow.Id] = workflow;
        return Task.CompletedTask;
    }
}
