using ArchLens.SharedKernel.Application;
using ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using MediatR;

namespace ArchLens.Upload.Application.UseCases.Diagrams.Queries.GetStatus;

public sealed class GetDiagramStatusHandler(
    IDiagramUploadRepository repository) : IRequestHandler<GetDiagramStatusQuery, Result<DiagramStatusResponse>>
{
    public async Task<Result<DiagramStatusResponse>> Handle(
        GetDiagramStatusQuery request,
        CancellationToken cancellationToken)
    {
        var diagram = await repository.GetByIdAsync(request.DiagramId, cancellationToken);

        if (diagram is null)
        {
            return Result.Failure<DiagramStatusResponse>(Error.NotFound);
        }

        return new DiagramStatusResponse(
            diagram.Id,
            diagram.FileName,
            diagram.FileType,
            diagram.FileSize,
            diagram.Status.Value,
            diagram.CreatedAt,
            diagram.UserId);
    }
}
