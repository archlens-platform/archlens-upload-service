using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;

namespace ArchLens.Upload.Tests.SharedKernel;

public class EntityTests
{
    [Fact]
    public void Equals_SameId_ShouldBeEqual()
    {
        var process1 = AnalysisProcess.Create(Guid.NewGuid());
        var process2 = process1;

        process1.Equals(process2).Should().BeTrue();
        (process1 == process2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ShouldNotBeEqual()
    {
        var process1 = AnalysisProcess.Create(Guid.NewGuid());
        var process2 = AnalysisProcess.Create(Guid.NewGuid());

        process1.Equals(process2).Should().BeFalse();
        (process1 != process2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());

        process.Equals(null).Should().BeFalse();
        (process == null).Should().BeFalse();
        (null == process).Should().BeFalse();
    }

    [Fact]
    public void Equals_BothNull_ShouldReturnTrue()
    {
        AnalysisProcess? a = null;
        AnalysisProcess? b = null;

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithObject_SameId_ShouldBeEqual()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());
        object obj = process;

        process.Equals(obj).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithObject_NonEntity_ShouldReturnFalse()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());

        process.Equals("not-an-entity").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameId_ShouldBeSame()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());

        process.GetHashCode().Should().Be(process.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentId_ShouldBeDifferent()
    {
        var process1 = AnalysisProcess.Create(Guid.NewGuid());
        var process2 = AnalysisProcess.Create(Guid.NewGuid());

        process1.GetHashCode().Should().NotBe(process2.GetHashCode());
    }

    [Fact]
    public void Equals_WithTypedNull_ShouldReturnFalse()
    {
        var process = AnalysisProcess.Create(Guid.NewGuid());
        AnalysisProcess? nullProcess = null;

        process.Equals(nullProcess).Should().BeFalse();
    }

    [Fact]
    public void NotEquals_Operator_DifferentIds_ShouldBeTrue()
    {
        var p1 = AnalysisProcess.Create(Guid.NewGuid());
        var p2 = AnalysisProcess.Create(Guid.NewGuid());

        (p1 != p2).Should().BeTrue();
    }

    [Fact]
    public void Equals_NullLeft_ShouldReturnFalse()
    {
        AnalysisProcess? left = null;
        var right = AnalysisProcess.Create(Guid.NewGuid());

        (left == right).Should().BeFalse();
        (left != right).Should().BeTrue();
    }
}
