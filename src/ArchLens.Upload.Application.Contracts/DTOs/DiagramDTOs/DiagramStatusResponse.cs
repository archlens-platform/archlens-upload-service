namespace ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;

public sealed record DiagramStatusResponse(
    Guid DiagramId,
    string FileName,
    string FileType,
    long FileSize,
    string Status,
    DateTime CreatedAt,
    string? UserId);
