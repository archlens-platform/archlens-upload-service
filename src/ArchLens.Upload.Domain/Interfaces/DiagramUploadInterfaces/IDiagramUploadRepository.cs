using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;

namespace ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;

public interface IDiagramUploadRepository : IRepository<DiagramUpload>
{
    Task<DiagramUpload?> GetByFileHashAsync(string fileHash, string? userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<DiagramUpload> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, string? userId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiagramUpload>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
