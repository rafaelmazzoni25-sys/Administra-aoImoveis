using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class DocumentRecord : Entity
{
    public DocumentRecord(
        Guid id,
        Guid ownerId,
        string ownerType,
        DocumentType type,
        string fileName,
        string storagePath,
        DateTime uploadedAt,
        DateTime? expiresAt) : base(id)
    {
        OwnerId = ownerId;
        OwnerType = ownerType;
        Type = type;
        FileName = fileName;
        StoragePath = storagePath;
        UploadedAt = uploadedAt;
        ExpiresAt = expiresAt;
    }

    public Guid OwnerId { get; }
    public string OwnerType { get; }
    public DocumentType Type { get; }
    public string FileName { get; }
    public string StoragePath { get; }
    public DateTime UploadedAt { get; }
    public DateTime? ExpiresAt { get; }
}
