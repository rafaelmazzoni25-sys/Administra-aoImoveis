using System.Linq;
using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class DocumentWorkflow : Entity
{
    private readonly List<DocumentSigner> _signers = new();
    private readonly List<DocumentWorkflowHistory> _history = new();

    public DocumentWorkflow(
        Guid id,
        Guid referenceId,
        string referenceType,
        DocumentType documentType,
        string title,
        string contentTemplate,
        IEnumerable<DocumentSigner> signers) : base(id)
    {
        ReferenceId = referenceId;
        ReferenceType = referenceType;
        DocumentType = documentType;
        Title = title;
        ContentTemplate = contentTemplate;
        Status = DocumentWorkflowStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        _signers.AddRange(signers);
        _history.Add(new DocumentWorkflowHistory(DateTime.UtcNow, Status, "Fluxo criado"));
    }

    public Guid ReferenceId { get; }
    public string ReferenceType { get; }
    public DocumentType DocumentType { get; }
    public string Title { get; private set; }
    public string ContentTemplate { get; private set; }
    public DocumentWorkflowStatus Status { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? CompletedAt { get; private set; }
    public IReadOnlyCollection<DocumentSigner> Signers => _signers.AsReadOnly();
    public IReadOnlyCollection<DocumentWorkflowHistory> History => _history.AsReadOnly();
    public DocumentRecord? GeneratedDocument { get; private set; }

    public void UpdateTemplate(string title, string contentTemplate)
    {
        if (Status != DocumentWorkflowStatus.Draft)
        {
            throw new InvalidOperationException("Apenas rascunhos podem ser editados.");
        }

        Title = title;
        ContentTemplate = contentTemplate;
        _history.Add(new DocumentWorkflowHistory(DateTime.UtcNow, Status, "Modelo atualizado"));
    }

    public void Activate(DocumentRecord generatedDocument)
    {
        if (Status != DocumentWorkflowStatus.Draft)
        {
            throw new InvalidOperationException("Fluxo já iniciado.");
        }

        GeneratedDocument = generatedDocument;
        Status = DocumentWorkflowStatus.PendingSignatures;
        _history.Add(new DocumentWorkflowHistory(DateTime.UtcNow, Status, "Documento gerado e aguardando assinaturas"));
    }

    public void RegisterSignature(Guid signerId, string signedFilePath)
    {
        if (Status is not DocumentWorkflowStatus.PendingSignatures)
        {
            throw new InvalidOperationException("Fluxo não está aguardando assinaturas.");
        }

        var signer = _signers.SingleOrDefault(s => s.Id == signerId)
            ?? throw new InvalidOperationException("Signatário não encontrado");

        signer.MarkSigned(signedFilePath);

        if (_signers.All(s => s.SignedAt.HasValue))
        {
            Status = DocumentWorkflowStatus.Signed;
            CompletedAt = DateTime.UtcNow;
            _history.Add(new DocumentWorkflowHistory(DateTime.UtcNow, Status, "Documento totalmente assinado"));
        }
        else
        {
            _history.Add(new DocumentWorkflowHistory(DateTime.UtcNow, Status, $"{signer.Name} assinou"));
        }
    }

    public void Archive(string reason)
    {
        if (Status == DocumentWorkflowStatus.Cancelled)
        {
            throw new InvalidOperationException("Fluxo cancelado não pode ser arquivado.");
        }

        Status = DocumentWorkflowStatus.Archived;
        CompletedAt = DateTime.UtcNow;
        _history.Add(new DocumentWorkflowHistory(DateTime.UtcNow, Status, reason));
    }

    public void Cancel(string reason)
    {
        Status = DocumentWorkflowStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        _history.Add(new DocumentWorkflowHistory(DateTime.UtcNow, Status, reason));
    }
}

public sealed class DocumentSigner
{
    public DocumentSigner(Guid id, string name, string email, bool mandatory)
    {
        Id = id;
        Name = name;
        Email = email;
        Mandatory = mandatory;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string Email { get; }
    public bool Mandatory { get; }
    public DateTime? SignedAt { get; private set; }
    public string? SignedFilePath { get; private set; }

    public void MarkSigned(string signedFilePath)
    {
        SignedAt = DateTime.UtcNow;
        SignedFilePath = signedFilePath;
    }
}

public sealed record DocumentWorkflowHistory(DateTime OccurredAt, DocumentWorkflowStatus Status, string Notes);
