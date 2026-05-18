using ArchLens.SharedKernel.Application;
using ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using MediatR;

namespace ArchLens.Upload.Application.UseCases.Diagrams.Queries.List;

public sealed class ListDiagramsHandler(
    IDiagramUploadRepository repository) : IRequestHandler<ListDiagramsQuery, Result<PagedResponse<DiagramStatusResponse>>>
{
    public async Task<Result<PagedResponse<DiagramStatusResponse>>> Handle(
        ListDiagramsQuery request,
        CancellationToken cancellationToken)
    {
        var paged = new PagedRequest(request.Page, request.PageSize);

        var effectiveUserId = request.IsAdmin ? null : request.UserId;

        var (items, totalCount) = await repository.GetPagedAsync(
            paged.Skip, paged.PageSize, effectiveUserId, cancellationToken);

        var dtos = items.Select(d => new DiagramStatusResponse(
            d.Id,
            d.FileName,
            d.FileType,
            d.FileSize,
            d.Status.Value,
            d.CreatedAt,
            d.UserId)).ToList();

        return new PagedResponse<DiagramStatusResponse>(dtos, paged.Page, paged.PageSize, totalCount);
    }
}
