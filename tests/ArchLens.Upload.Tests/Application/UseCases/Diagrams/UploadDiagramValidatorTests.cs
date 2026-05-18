using ArchLens.Upload.Application.UseCases.Diagrams.Commands.Upload;
using ArchLens.Upload.Application.UseCases.Diagrams.Validators;
using FluentAssertions;
using FluentValidation;

namespace ArchLens.Upload.Tests.Application.UseCases.Diagrams;

public class UploadDiagramValidatorTests
{
    private readonly UploadDiagramValidator _validator = new();

    private static UploadDiagramCommand ValidCommand(
        string fileName = "diagram.png",
        string contentType = "image/png",
        long fileSize = 1024) =>
        new(new MemoryStream([0x01]), fileName, contentType, fileSize);

    // ─── FileName ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("diagram.png", "image/png")]
    [InlineData("arch.jpg", "image/jpeg")]
    [InlineData("schema.jpeg", "image/jpeg")]
    [InlineData("flow.webp", "image/webp")]
    [InlineData("report.pdf", "application/pdf")]
    public async Task Validate_AllowedExtensions_ShouldPass(string fileName, string contentType)
    {
        var result = await _validator.ValidateAsync(ValidCommand(fileName, contentType));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.bat")]
    [InlineData("archive.zip")]
    [InlineData("document.docx")]
    [InlineData("spreadsheet.xlsx")]
    [InlineData("image.bmp")]
    public async Task Validate_DisallowedExtensions_ShouldFail(string fileName)
    {
        var result = await _validator.ValidateAsync(ValidCommand(fileName));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EmptyFileName_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidCommand(""));
        result.IsValid.Should().BeFalse();
    }

    // ─── ContentType ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("text/plain")]
    [InlineData("image/gif")]
    [InlineData("video/mp4")]
    public async Task Validate_DisallowedContentType_ShouldFail(string contentType)
    {
        var result = await _validator.ValidateAsync(ValidCommand(contentType: contentType));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EmptyContentType_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidCommand(contentType: ""));
        result.IsValid.Should().BeFalse();
    }

    // ─── FileSize ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_ZeroFileSize_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidCommand(fileSize: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NegativeFileSize_ShouldFail()
    {
        var result = await _validator.ValidateAsync(ValidCommand(fileSize: -1));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ExactlyMaxFileSize_ShouldPass()
    {
        const long max = 20 * 1024 * 1024;
        var result = await _validator.ValidateAsync(ValidCommand(fileSize: max));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ExceedsMaxFileSize_ShouldFail()
    {
        const long overMax = 20 * 1024 * 1024 + 1;
        var result = await _validator.ValidateAsync(ValidCommand(fileSize: overMax));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_OneByteFile_ShouldPass()
    {
        var result = await _validator.ValidateAsync(ValidCommand(fileSize: 1));
        result.IsValid.Should().BeTrue();
    }

    // ─── Combined ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_AllInvalid_ShouldReturnMultipleErrors()
    {
        var cmd = new UploadDiagramCommand(new MemoryStream([0x01]), "bad.exe", "text/plain", 0);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
    }
}
