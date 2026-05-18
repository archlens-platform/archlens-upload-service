using ArchLens.SharedKernel.Domain;

namespace ArchLens.Upload.Domain.ValueObjects.Diagrams;

public sealed class DiagramStatus : ValueObject
{
    public static readonly DiagramStatus Received = new("Received");
    public static readonly DiagramStatus Processing = new("Processing");
    public static readonly DiagramStatus Analyzed = new("Analyzed");
    public static readonly DiagramStatus Error = new("Error");

    public string Value { get; }

    private DiagramStatus(string value)
    {
        Value = value;
    }

    public static DiagramStatus FromString(string status)
    {
        return status switch
        {
            "Received" => Received,
            "Processing" => Processing,
            "Analyzed" => Analyzed,
            "Error" => Error,
            _ => throw new ArgumentException($"Invalid status: {status}", nameof(status))
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
