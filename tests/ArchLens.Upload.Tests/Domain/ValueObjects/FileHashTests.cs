using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using FluentAssertions;

namespace ArchLens.Upload.Tests.Domain.ValueObjects;

public class FileHashTests
{
    [Fact]
    public void Create_ShouldGenerate_DeterministicHash()
    {
        var bytes = "hello world"u8.ToArray();

        var hash1 = FileHash.Create(bytes);
        var hash2 = FileHash.Create(bytes);

        hash1.Should().Be(hash2);
        hash1.Value.Should().HaveLength(64);
    }

    [Fact]
    public void Create_DifferentContent_ShouldProduceDifferentHash()
    {
        var hash1 = FileHash.Create("content-a"u8.ToArray());
        var hash2 = FileHash.Create("content-b"u8.ToArray());

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void FromString_WithValidHash_ShouldSucceed()
    {
        var original = FileHash.Create("test"u8.ToArray());

        var reconstructed = FileHash.FromString(original.Value);

        reconstructed.Should().Be(original);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromString_WithInvalidInput_ShouldThrow(string? input)
    {
        var act = () => FileHash.FromString(input!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromString_ShouldNormalize_ToLowercase()
    {
        var hash = FileHash.FromString("ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890");

        hash.Value.Should().Be("abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890");
    }

    [Fact]
    public void ToString_ShouldReturn_HashValue()
    {
        var hash = FileHash.Create("test"u8.ToArray());

        hash.ToString().Should().Be(hash.Value);
    }
}
