using ArchLens.SharedKernel.Domain;

namespace ArchLens.Upload.Domain.ValueObjects.Diagrams;

public sealed class FileHash : ValueObject
{
    public string Value { get; }

    private FileHash(string value)
    {
        Value = value;
    }

    public static FileHash Create(byte[] fileBytes)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(fileBytes);
        return new FileHash(Convert.ToHexStringLower(hash));
    }

    public static FileHash FromString(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return new FileHash(hash.ToLowerInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
