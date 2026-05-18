using ArchLens.SharedKernel.Domain;

namespace ArchLens.Upload.Domain.Exceptions;

public sealed class FileTooLargeException : DomainException
{
    public FileTooLargeException(long fileSize, long maxSize)
        : base("Upload.FileTooLarge", $"File size {fileSize} bytes exceeds maximum allowed size of {maxSize} bytes")
    {
    }
}
