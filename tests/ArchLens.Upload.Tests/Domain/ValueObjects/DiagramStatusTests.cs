using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Domain.ValueObjects;

public class DiagramStatusTests
{
    [Theory]
    [InlineData("Received")]
    [InlineData("Processing")]
    [InlineData("Analyzed")]
    [InlineData("Error")]
    public void FromString_WithValidStatus_ShouldReturnCorrectInstance(string status)
    {
        var result = DiagramStatus.FromString(status);

        result.Value.Should().Be(status);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("RECEIVED")]
    public void FromString_WithInvalidStatus_ShouldThrow(string status)
    {
        var act = () => DiagramStatus.FromString(status);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameStatus_ShouldBeEqual()
    {
        var a = DiagramStatus.Received;
        var b = DiagramStatus.Received;

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentStatus_ShouldNotBeEqual()
    {
        DiagramStatus.Received.Should().NotBe(DiagramStatus.Processing);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        DiagramStatus.Received.ToString().Should().Be("Received");
    }
}
