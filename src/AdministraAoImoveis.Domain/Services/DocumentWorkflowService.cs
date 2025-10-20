using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Repositories;

namespace AdministraAoImoveis.Domain.Services;

public sealed class DocumentWorkflowService
{
    private readonly IDocumentWorkflowRepository _workflowRepository;
    private readonly IDocumentRepository _documentRepository;

    public DocumentWorkflowService(IDocumentWorkflowRepository workflowRepository, IDocumentRepository documentRepository)
    {
        _workflowRepository = workflowRepository;
        _documentRepository = documentRepository;
    }

    public async Task<DocumentWorkflow> CreateAsync(DocumentWorkflow workflow, CancellationToken cancellationToken = default)
    {
        await _workflowRepository.AddAsync(workflow, cancellationToken);
        return workflow;
    }

    public async Task<DocumentWorkflow> UpdateTemplateAsync(Guid workflowId, string title, string template, CancellationToken cancellationToken = default)
    {
        var workflow = await EnsureWorkflowAsync(workflowId, cancellationToken);
        workflow.UpdateTemplate(title, template);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        return workflow;
    }

    public async Task<DocumentWorkflow> ActivateAsync(Guid workflowId, DocumentRecord record, CancellationToken cancellationToken = default)
    {
        var workflow = await EnsureWorkflowAsync(workflowId, cancellationToken);
        workflow.Activate(record);
        await _documentRepository.AddAsync(record, cancellationToken);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        return workflow;
    }

    public async Task<DocumentWorkflow> RegisterSignatureAsync(Guid workflowId, Guid signerId, string filePath, CancellationToken cancellationToken = default)
    {
        var workflow = await EnsureWorkflowAsync(workflowId, cancellationToken);
        workflow.RegisterSignature(signerId, filePath);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        return workflow;
    }

    public async Task<DocumentWorkflow> ArchiveAsync(Guid workflowId, string reason, CancellationToken cancellationToken = default)
    {
        var workflow = await EnsureWorkflowAsync(workflowId, cancellationToken);
        workflow.Archive(reason);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        return workflow;
    }

    public async Task<DocumentWorkflow> CancelAsync(Guid workflowId, string reason, CancellationToken cancellationToken = default)
    {
        var workflow = await EnsureWorkflowAsync(workflowId, cancellationToken);
        workflow.Cancel(reason);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        return workflow;
    }

    private async Task<DocumentWorkflow> EnsureWorkflowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _workflowRepository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Fluxo não localizado");
    }
}
