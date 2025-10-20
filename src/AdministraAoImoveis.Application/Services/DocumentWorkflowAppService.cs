using System.Linq;
using AdministraAoImoveis.Application.Abstractions;
using AdministraAoImoveis.Application.DTOs;
using AdministraAoImoveis.Domain.Entities;
using AdministraAoImoveis.Domain.Enums;
using AdministraAoImoveis.Domain.Repositories;
using AdministraAoImoveis.Domain.Services;

namespace AdministraAoImoveis.Application.Services;

public sealed class DocumentWorkflowAppService
{
    private readonly DocumentWorkflowService _documentWorkflowService;
    private readonly IDocumentWorkflowRepository _documentWorkflowRepository;

    public DocumentWorkflowAppService(DocumentWorkflowService documentWorkflowService, IDocumentWorkflowRepository documentWorkflowRepository)
    {
        _documentWorkflowService = documentWorkflowService;
        _documentWorkflowRepository = documentWorkflowRepository;
    }

    public async Task<DocumentWorkflowDto> CreateAsync(Guid referenceId, string referenceType, DocumentType documentType, string title, string contentTemplate, IEnumerable<DocumentSignerDto> signers, CancellationToken cancellationToken = default)
    {
        var workflow = new DocumentWorkflow(
            Guid.NewGuid(),
            referenceId,
            referenceType,
            documentType,
            title,
            contentTemplate,
            signers.Select(s =>
            {
                var signerId = s.Id.HasValue && s.Id.Value != Guid.Empty ? s.Id.Value : Guid.NewGuid();
                return new DocumentSigner(signerId, s.Name, s.Email, s.Mandatory);
            }));

        await _documentWorkflowService.CreateAsync(workflow, cancellationToken);
        return workflow.ToDto();
    }

    public async Task<DocumentWorkflowDto> UpdateTemplateAsync(Guid workflowId, string title, string template, CancellationToken cancellationToken = default)
    {
        var workflow = await _documentWorkflowService.UpdateTemplateAsync(workflowId, title, template, cancellationToken);
        return workflow.ToDto();
    }

    public async Task<DocumentWorkflowDto> ActivateAsync(Guid workflowId, string fileName, string storagePath, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        var workflow = await _documentWorkflowRepository.GetByIdAsync(workflowId, cancellationToken) ?? throw new InvalidOperationException("Fluxo não encontrado");
        var record = new DocumentRecord(Guid.NewGuid(), workflow.ReferenceId, workflow.ReferenceType, workflow.DocumentType, fileName, storagePath, DateTime.UtcNow, expiresAt);
        var updated = await _documentWorkflowService.ActivateAsync(workflowId, record, cancellationToken);
        return updated.ToDto();
    }

    public async Task<DocumentWorkflowDto> RegisterSignatureAsync(Guid workflowId, Guid signerId, string filePath, CancellationToken cancellationToken = default)
    {
        var workflow = await _documentWorkflowService.RegisterSignatureAsync(workflowId, signerId, filePath, cancellationToken);
        return workflow.ToDto();
    }

    public async Task<DocumentWorkflowDto> ArchiveAsync(Guid workflowId, string reason, CancellationToken cancellationToken = default)
    {
        var workflow = await _documentWorkflowService.ArchiveAsync(workflowId, reason, cancellationToken);
        return workflow.ToDto();
    }

    public async Task<DocumentWorkflowDto> CancelAsync(Guid workflowId, string reason, CancellationToken cancellationToken = default)
    {
        var workflow = await _documentWorkflowService.CancelAsync(workflowId, reason, cancellationToken);
        return workflow.ToDto();
    }

    public async Task<IReadOnlyCollection<DocumentWorkflowDto>> GetByReferenceAsync(Guid referenceId, string referenceType, CancellationToken cancellationToken = default)
    {
        var workflows = await _documentWorkflowRepository.GetByReferenceAsync(referenceId, referenceType, cancellationToken);
        return workflows.Select(w => w.ToDto()).ToList();
    }
}
