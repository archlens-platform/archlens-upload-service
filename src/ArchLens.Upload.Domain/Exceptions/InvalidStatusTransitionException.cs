using ArchLens.SharedKernel.Domain;

namespace ArchLens.Upload.Domain.Exceptions;

public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(string currentStatus, string targetStatus)
        : base("Upload.InvalidStatusTransition", $"Cannot transition from '{currentStatus}' to '{targetStatus}'")
    {
    }
}
