using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;

namespace ArchLens.Upload.Tests.SharedKernel;

public class ValueObjectTests
{
    [Fact]
    public void Equals_SameValues_ShouldBeEqual()
    {
        var hash1 = FileHash.FromString("abc123");
        var hash2 = FileHash.FromString("abc123");

        hash1.Equals(hash2).Should().BeTrue();
        (hash1 == hash2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValues_ShouldNotBeEqual()
    {
        var hash1 = FileHash.FromString("abc123");
        var hash2 = FileHash.FromString("xyz789");

        hash1.Equals(hash2).Should().BeFalse();
        (hash1 != hash2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var hash = FileHash.FromString("abc");

        hash.Equals(null).Should().BeFalse();
        (hash == null).Should().BeFalse();
        (null == hash).Should().BeFalse();
    }

    [Fact]
    public void Equals_BothNull_ShouldReturnTrue()
    {
        FileHash? a = null;
        FileHash? b = null;

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithObjectOfDifferentType_ShouldReturnFalse()
    {
        var hash = FileHash.FromString("abc");

        hash.Equals("abc").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldBeSame()
    {
        var hash1 = FileHash.FromString("abc");
        var hash2 = FileHash.FromString("abc");

        hash1.GetHashCode().Should().Be(hash2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ShouldBeDifferent()
    {
        var hash1 = FileHash.FromString("abc");
        var hash2 = FileHash.FromString("xyz");

        hash1.GetHashCode().Should().NotBe(hash2.GetHashCode());
    }

    [Fact]
    public void Equals_DiagramStatus_SameValues_ShouldBeEqual()
    {
        var status1 = DiagramStatus.Received;
        var status2 = DiagramStatus.FromString("Received");

        status1.Equals(status2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DiagramStatus_AsObject_ShouldBeEqual()
    {
        var status = DiagramStatus.Processing;
        object obj = DiagramStatus.FromString("Processing");

        status.Equals(obj).Should().BeTrue();
    }

    [Fact]
    public void NotEquals_Operator_ShouldWork()
    {
        (DiagramStatus.Received != DiagramStatus.Error).Should().BeTrue();
        (DiagramStatus.Received != DiagramStatus.Received).Should().BeFalse();
    }
}
