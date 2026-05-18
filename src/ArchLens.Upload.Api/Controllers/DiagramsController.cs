using System.Security.Claims;
using System.Text.RegularExpressions;
using ArchLens.Upload.Api.Filters;
using ArchLens.Upload.Application.UseCases.Diagrams.Commands.Upload;
using ArchLens.Upload.Application.UseCases.Diagrams.Queries.GetStatus;
using ArchLens.Upload.Application.UseCases.Diagrams.Queries.List;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLens.Upload.Api.Controllers;

[ApiController]
[Route("diagrams")]
[Authorize]
public sealed class DiagramsController(ISender sender, IDiagramUploadRepository diagramRepo, ArchLens.SharedKernel.Domain.IUnitOfWork unitOfWork, IFileStorageService fileStorage) : ControllerBase
{
    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    private bool IsAdmin() => User.IsInRole("Admin");

    [HttpPost]
    [ValidateFileSignature]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        var safeFileName = SanitizeFileName(file.FileName);

        var command = new UploadDiagramCommand(
            file.OpenReadStream(),
            safeFileName,
            file.ContentType,
            file.Length,
            GetCurrentUserId());

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Code, message = result.Error.Description });
        }

        return CreatedAtAction(
            nameof(GetStatus),
            new { id = result.Value.DiagramId },
            result.Value);
    }

    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDiagramStatusQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Code, message = result.Error.Description });
        }

        if (!IsAdmin() && result.Value.UserId != GetCurrentUserId())
            return NotFound();

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDiagramStatusQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Code, message = result.Error.Description });
        }

        if (!IsAdmin() && result.Value.UserId != GetCurrentUserId())
            return NotFound();

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken)
    {
        var diagram = await diagramRepo.GetByIdAsync(id, cancellationToken);
        if (diagram is null) return NotFound();

        if (!IsAdmin() && diagram.UserId != GetCurrentUserId())
            return NotFound();

        var stream = await fileStorage.DownloadAsync(diagram.StoragePath, cancellationToken);
        return File(stream, diagram.FileType, diagram.FileName);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListDiagramsQuery(page, pageSize, GetCurrentUserId(), false), cancellationToken);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var diagram = await diagramRepo.GetByIdAsync(id, cancellationToken);
        if (diagram is null) return NotFound();

        if (!IsAdmin() && diagram.UserId != GetCurrentUserId())
            return NotFound();

        await diagramRepo.DeleteAsync(diagram, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string SanitizeFileName(string fileName)
    {

        var name = Path.GetFileName(fileName);

        name = name.Replace("\0", string.Empty, StringComparison.Ordinal);
        name = Regex.Replace(name, @"[\x00-\x1f\x7f]", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));

        var ext = Path.GetExtension(name);
        var stem = Path.GetFileNameWithoutExtension(name);
        stem = Regex.Replace(stem, @"[^\w\-]", "_", RegexOptions.None, TimeSpan.FromSeconds(1));

        return string.IsNullOrWhiteSpace(stem) ? $"upload{ext}" : $"{stem}{ext}";
    }
}
