using ArchLens.SharedKernel.Application;
using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ArchLens.Upload.Application.UseCases.Diagrams.Commands.Upload;

public sealed class UploadDiagramHandler(
    IDiagramUploadRepository diagramRepository,
    IFileStorageService fileStorage,
    IUnitOfWork unitOfWork,
    ILogger<UploadDiagramHandler> logger) : IRequestHandler<UploadDiagramCommand, Result<UploadDiagramResponse>>
{
    public async Task<Result<UploadDiagramResponse>> Handle(
        UploadDiagramCommand request,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await request.FileStream.CopyToAsync(memoryStream, cancellationToken);
        var fileBytes = memoryStream.ToArray();

        var fileHash = FileHash.Create(fileBytes);

        var existing = await diagramRepository.GetByFileHashAsync(fileHash.Value, request.UserId, cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation("Duplicate diagram detected, returning existing: {DiagramId} {FileHash}", existing.Id, fileHash.Value);
            return new UploadDiagramResponse(
                existing.Id,
                existing.FileName,
                existing.Status.Value,
                existing.CreatedAt,
                IsDuplicate: true);
        }

        memoryStream.Position = 0;
        var storagePath = await fileStorage.UploadAsync(
            memoryStream, request.FileName, request.ContentType, cancellationToken);

        var diagram = DiagramUpload.Create(
            request.FileName,
            request.ContentType,
            request.FileSize,
            fileHash,
            storagePath,
            request.UserId);

        await diagramRepository.AddAsync(diagram, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Diagram uploaded: {DiagramId} {FileName}", diagram.Id, diagram.FileName);

        return new UploadDiagramResponse(
            diagram.Id,
            diagram.FileName,
            diagram.Status.Value,
            diagram.CreatedAt);
    }
}
