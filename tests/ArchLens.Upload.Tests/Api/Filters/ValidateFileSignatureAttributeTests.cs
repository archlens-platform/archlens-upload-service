using ArchLens.Upload.Api.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

namespace ArchLens.Upload.Tests.Api.Filters;

public class ValidateFileSignatureAttributeTests
{
    private readonly ValidateFileSignatureAttribute _attribute = new();

    private ActionExecutingContext CreateContext(Dictionary<string, object?> actionArguments)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments!,
            controller: null!);
    }

    private static IFormFile CreateFormFile(string fileName, byte[] content)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.OpenReadStream().Returns(new MemoryStream(content));
        return file;
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoFileArgument_ShouldCallNext()
    {
        var context = CreateContext(new Dictionary<string, object?>());
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnActionExecutionAsync_NonFormFileArgument_ShouldCallNext()
    {
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = "not-a-file" });
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidPng_ShouldCallNext()
    {
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var file = CreateFormFile("test.png", pngSignature);
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidJpg_ShouldCallNext()
    {
        var jpgSignature = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var file = CreateFormFile("photo.jpg", jpgSignature);
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidPdf_ShouldCallNext()
    {
        var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x35 };
        var file = CreateFormFile("doc.pdf", pdfSignature);
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidSvgWithSvgTag_ShouldCallNext()
    {
        // <svg starts with 0x3C, 0x73, 0x76, 0x67
        var svgContent = "<svg xmlns=\"http://www.w3.org/2000/svg\">"u8.ToArray();
        var file = CreateFormFile("diagram.svg", svgContent);
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidSvgWithXmlDeclaration_ShouldCallNext()
    {
        // <?xml starts with 0x3C, 0x3F, 0x78, 0x6D, 0x6C
        var svgXml = "<?xml version=\"1.0\"?><svg></svg>"u8.ToArray();
        var file = CreateFormFile("diagram.svg", svgXml);
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnActionExecutionAsync_UnsupportedExtension_ShouldReturn415()
    {
        var file = CreateFormFile("file.exe", new byte[] { 0x4D, 0x5A, 0x90, 0x00 });
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });

        await _attribute.OnActionExecutionAsync(context, () =>
            Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!)));

        context.Result.Should().NotBeNull();
        var objectResult = context.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoExtension_ShouldReturn415()
    {
        var file = CreateFormFile("noextension", new byte[] { 0x00, 0x00, 0x00, 0x00 });
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });

        await _attribute.OnActionExecutionAsync(context, () =>
            Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!)));

        context.Result.Should().NotBeNull();
        var objectResult = context.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WrongSignatureForPng_ShouldReturn415()
    {
        // File says .png but bytes are JPEG signature
        var file = CreateFormFile("fake.png", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 });
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });

        await _attribute.OnActionExecutionAsync(context, () =>
            Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!)));

        context.Result.Should().NotBeNull();
        var objectResult = context.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidJpeg_ShouldCallNext()
    {
        var jpegSignature = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1, 0x00, 0x10, 0x45, 0x78 };
        var file = CreateFormFile("image.jpeg", jpegSignature);
        var context = CreateContext(new Dictionary<string, object?> { ["file"] = file });
        var nextCalled = false;

        await _attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                controller: null!));
        });

        nextCalled.Should().BeTrue();
    }
}
