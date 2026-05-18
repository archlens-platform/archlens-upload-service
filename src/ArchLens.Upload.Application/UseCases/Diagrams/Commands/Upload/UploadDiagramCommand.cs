using ArchLens.SharedKernel.Application;
using ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;
using MediatR;

namespace ArchLens.Upload.Application.UseCases.Diagrams.Commands.Upload;

public sealed record UploadDiagramCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize,
    string? UserId = null) : IRequest<Result<UploadDiagramResponse>>;
