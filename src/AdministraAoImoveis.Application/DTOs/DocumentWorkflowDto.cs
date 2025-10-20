using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Application.DTOs;

public sealed record DocumentWorkflowDto(
    Guid Id,
    Guid ReferenceId,
    string ReferenceType,
    DocumentType DocumentType,
    string Title,
    string ContentTemplate,
    DocumentWorkflowStatus Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    IReadOnlyCollection<DocumentSignerDto> Signers,
    IReadOnlyCollection<DocumentWorkflowHistoryDto> History,
    DocumentRecordDto? GeneratedDocument);

public sealed record DocumentSignerDto(Guid Id, string Name, string Email, bool Mandatory, DateTime? SignedAt, string? SignedFilePath);
public sealed record DocumentWorkflowHistoryDto(DateTime OccurredAt, DocumentWorkflowStatus Status, string Notes);
public sealed record DocumentRecordDto(Guid Id, Guid OwnerId, string OwnerType, DocumentType Type, string FileName, string StoragePath, DateTime UploadedAt, DateTime? ExpiresAt);
