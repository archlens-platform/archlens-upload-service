using ArchLens.SharedKernel.Application;
using ArchLens.Upload.Application.UseCases.Diagrams.Queries.GetStatus;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;
using NSubstitute;

namespace ArchLens.Upload.Tests.Application.UseCases.Diagrams;

public class GetDiagramStatusHandlerTests
{
    private readonly IDiagramUploadRepository _repository = Substitute.For<IDiagramUploadRepository>();
    private readonly GetDiagramStatusHandler _handler;

    public GetDiagramStatusHandlerTests()
    {
        _handler = new GetDiagramStatusHandler(_repository);
    }

    [Fact]
    public async Task Handle_ExistingDiagram_ShouldReturnSuccess()
    {
        var diagram = DiagramUpload.Create("test.png", "image/png", 1024,
            FileHash.Create("bytes"u8.ToArray()), "bucket/key");

        _repository.GetByIdAsync(diagram.Id, Arg.Any<CancellationToken>())
            .Returns(diagram);

        var result = await _handler.Handle(new GetDiagramStatusQuery(diagram.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("test.png");
        result.Value.Status.Should().Be("Received");
    }

    [Fact]
    public async Task Handle_NonExistingDiagram_ShouldReturnNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DiagramUpload?)null);

        var result = await _handler.Handle(new GetDiagramStatusQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NotFound);
    }
}
