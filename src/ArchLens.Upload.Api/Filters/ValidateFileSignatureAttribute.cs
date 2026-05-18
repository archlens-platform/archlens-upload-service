using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchLens.Upload.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ValidateFileSignatureAttribute : Attribute, IAsyncActionFilter
{
    private static readonly Dictionary<string, byte[][]> AllowedSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"]  = [[0x89, 0x50, 0x4E, 0x47]],
        [".jpg"]  = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".svg"]  = [[0x3C, 0x73, 0x76, 0x67], [0x3C, 0x3F, 0x78, 0x6D, 0x6C]],
        [".pdf"]  = [[0x25, 0x50, 0x44, 0x46]],
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("file", out var fileObj)
            || fileObj is not IFormFile file)
        {
            await next();
            return;
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(ext) || !AllowedSignatures.TryGetValue(ext, out var validSignatures))
        {
            context.Result = new ObjectResult(new
            {
                error = "INVALID_FILE_TYPE",
                message = "File type is not allowed. Accepted: .png, .jpg, .jpeg, .svg, .pdf"
            })
            { StatusCode = StatusCodes.Status415UnsupportedMediaType };
            return;
        }

        using var stream = file.OpenReadStream();
        var buffer = new byte[8];
        var bytesRead = await stream.ReadAsync(buffer);

        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        var isValid = validSignatures.Any(signature =>
            bytesRead >= signature.Length &&
            buffer.AsSpan(0, signature.Length).SequenceEqual(signature));

        if (!isValid)
        {
            context.Result = new ObjectResult(new
            {
                error = "INVALID_FILE_SIGNATURE",
                message = "File content does not match its declared extension."
            })
            { StatusCode = StatusCodes.Status415UnsupportedMediaType };
            return;
        }

        await next();
    }
}
