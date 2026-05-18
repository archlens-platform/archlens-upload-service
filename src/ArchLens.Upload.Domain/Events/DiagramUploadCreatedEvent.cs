using ArchLens.SharedKernel.Domain;

namespace ArchLens.Upload.Domain.Events;

public sealed record DiagramUploadCreatedEvent(
    Guid DiagramId,
    string FileName,
    string FileHash,
    string StoragePath,
    string? UserId) : DomainEvent;
