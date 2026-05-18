using ArchLens.SharedKernel.Application;
using ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;
using MediatR;

namespace ArchLens.Upload.Application.UseCases.Diagrams.Queries.List;

public sealed record ListDiagramsQuery(
    int Page = 1,
    int PageSize = 20,
    string? UserId = null,
    bool IsAdmin = false) : IRequest<Result<PagedResponse<DiagramStatusResponse>>>;
