using ArchLens.Upload.Application.UseCases.Diagrams.Commands.Upload;
using FluentValidation;

namespace ArchLens.Upload.Application.UseCases.Diagrams.Validators;

public sealed class UploadDiagramValidator : AbstractValidator<UploadDiagramCommand>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/png",
        "image/jpeg",
        "image/webp",
        "application/pdf"
    ];

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".webp", ".pdf"
    ];

    private const long MaxFileSize = 20 * 1024 * 1024;

    public UploadDiagramValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("Supported formats: PNG, JPG, JPEG, WEBP, PDF");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(AllowedContentTypes.Contains)
            .WithMessage("Invalid content type");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage($"File size must be less than {MaxFileSize / 1024 / 1024}MB");

        RuleFor(x => x.FileStream)
            .NotNull();
    }
}
