using ArchLens.Upload.Application.UseCases.Diagrams.Queries.List;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;
using NSubstitute;

namespace ArchLens.Upload.Tests.Application.UseCases.Diagrams;

public class ListDiagramsHandlerTests
{
    private readonly IDiagramUploadRepository _repository = Substitute.For<IDiagramUploadRepository>();
    private readonly ListDiagramsHandler _handler;

    public ListDiagramsHandlerTests()
    {
        _handler = new ListDiagramsHandler(_repository);
    }

    private static DiagramUpload CreateDiagram(string? userId = null) =>
        DiagramUpload.Create("diagram.png", "image/png", 2048,
            FileHash.Create(Guid.NewGuid().ToByteArray()), "bucket/path", userId);

    [Fact]
    public async Task Handle_WithItems_ShouldReturnPagedResponse()
    {
        var items = new List<DiagramUpload> { CreateDiagram(), CreateDiagram() };
        _repository.GetPagedAsync(0, 20, null, Arg.Any<CancellationToken>())
            .Returns((items as IReadOnlyList<DiagramUpload>, 2));

        var result = await _handler.Handle(new ListDiagramsQuery(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ShouldReturnEmptyPage()
    {
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload>() as IReadOnlyList<DiagramUpload>, 0));

        var result = await _handler.Handle(new ListDiagramsQuery(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithUserId_ShouldPassUserIdToRepository()
    {
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), "user-42", Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload>() as IReadOnlyList<DiagramUpload>, 0));

        await _handler.Handle(new ListDiagramsQuery(1, 20, "user-42"), CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(
            Arg.Any<int>(), Arg.Any<int>(), "user-42", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Page2_ShouldPassCorrectSkipToRepository()
    {
        _repository.GetPagedAsync(20, 20, null, Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload>() as IReadOnlyList<DiagramUpload>, 50));

        var result = await _handler.Handle(new ListDiagramsQuery(2, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).GetPagedAsync(20, 20, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldMapDiagram_ToStatusResponse()
    {
        var diagram = CreateDiagram("user-1");
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload> { diagram } as IReadOnlyList<DiagramUpload>, 1));

        var result = await _handler.Handle(new ListDiagramsQuery(), CancellationToken.None);

        var item = result.Value.Items.Single();
        item.DiagramId.Should().Be(diagram.Id);
        item.FileName.Should().Be("diagram.png");
        item.Status.Should().Be("Received");
        item.UserId.Should().Be("user-1");
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(3, 5)]
    [InlineData(1, 100)]
    public async Task Handle_DifferentPageSizes_ShouldPassCorrectParameters(int page, int pageSize)
    {
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<DiagramUpload>() as IReadOnlyList<DiagramUpload>, 0));

        await _handler.Handle(new ListDiagramsQuery(page, pageSize), CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(
            (page - 1) * pageSize, pageSize, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
