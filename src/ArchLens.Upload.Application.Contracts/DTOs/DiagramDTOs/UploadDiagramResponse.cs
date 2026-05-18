namespace ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;

public sealed record UploadDiagramResponse(
    Guid DiagramId,
    string FileName,
    string Status,
    DateTime CreatedAt,
    bool IsDuplicate = false);
