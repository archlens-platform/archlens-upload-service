using ArchLens.SharedKernel.Domain;

namespace ArchLens.Upload.Domain.Exceptions;

public sealed class InvalidFileTypeException : DomainException
{
    public InvalidFileTypeException(string extension)
        : base("Upload.InvalidFileType", $"File type '{extension}' is not supported. Allowed: .png, .jpg, .jpeg, .webp, .pdf")
    {
    }
}
