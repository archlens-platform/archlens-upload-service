using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Events;
using ArchLens.Upload.Domain.Exceptions;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;

namespace ArchLens.Upload.Domain.Entities.DiagramUploadEntities;

public sealed class DiagramUpload : AggregateRoot<Guid>
{
    private static readonly HashSet<string> _allowedExtensions =
        [".png", ".jpg", ".jpeg", ".webp", ".pdf"];

    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    public string FileName { get; private set; } = string.Empty;
    public string FileType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public FileHash FileHash { get; private set; } = null!;
    public string StoragePath { get; private set; } = string.Empty;
    public DiagramStatus Status { get; private set; } = DiagramStatus.Received;
    public DateTime CreatedAt { get; private set; }
    public string? UserId { get; private set; }

    private DiagramUpload() { }

    public static DiagramUpload Create(
        string fileName,
        string fileType,
        long fileSize,
        FileHash fileHash,
        string storagePath,
        string? userId = null)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            throw new InvalidFileTypeException(extension);
        }

        if (fileSize <= 0 || fileSize > MaxFileSizeBytes)
        {
            throw new FileTooLargeException(fileSize, MaxFileSizeBytes);
        }

        var diagram = new DiagramUpload
        {
            Id = Guid.NewGuid(),
            FileName = Path.GetFileName(fileName),
            FileType = fileType,
            FileSize = fileSize,
            FileHash = fileHash,
            StoragePath = storagePath,
            Status = DiagramStatus.Received,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        diagram.RaiseDomainEvent(new DiagramUploadCreatedEvent(
            diagram.Id,
            diagram.FileName,
            diagram.FileHash.Value,
            diagram.StoragePath,
            diagram.UserId));

        return diagram;
    }

    public void MarkAsProcessing()
    {
        if (Status != DiagramStatus.Received)
        {
            throw new InvalidStatusTransitionException(Status.Value, DiagramStatus.Processing.Value);
        }

        var oldStatus = Status.Value;
        Status = DiagramStatus.Processing;
        RaiseDomainEvent(new DiagramStatusChangedEvent(Id, oldStatus, Status.Value));
    }

    public void MarkAsAnalyzed()
    {
        if (Status != DiagramStatus.Processing)
        {
            throw new InvalidStatusTransitionException(Status.Value, DiagramStatus.Analyzed.Value);
        }

        var oldStatus = Status.Value;
        Status = DiagramStatus.Analyzed;
        RaiseDomainEvent(new DiagramStatusChangedEvent(Id, oldStatus, Status.Value));
    }

    public void MarkAsError()
    {
        var oldStatus = Status.Value;
        Status = DiagramStatus.Error;
        RaiseDomainEvent(new DiagramStatusChangedEvent(Id, oldStatus, Status.Value));
    }
}
