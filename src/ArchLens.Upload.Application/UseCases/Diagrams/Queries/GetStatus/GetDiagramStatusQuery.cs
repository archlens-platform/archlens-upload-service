using ArchLens.SharedKernel.Application;
using ArchLens.Upload.Application.Contracts.DTOs.DiagramDTOs;
using MediatR;

namespace ArchLens.Upload.Application.UseCases.Diagrams.Queries.GetStatus;

public sealed record GetDiagramStatusQuery(Guid DiagramId) : IRequest<Result<DiagramStatusResponse>>;
