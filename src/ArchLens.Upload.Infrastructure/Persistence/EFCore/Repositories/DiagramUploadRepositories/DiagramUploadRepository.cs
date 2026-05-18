using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Upload.Infrastructure.Persistence.EFCore.Repositories.DiagramUploadRepositories;

public sealed class DiagramUploadRepository(UploadDbContext context) : IDiagramUploadRepository
{
    public async Task<DiagramUpload?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.DiagramUploads.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(DiagramUpload entity, CancellationToken cancellationToken = default)
    {
        await context.DiagramUploads.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(DiagramUpload entity, CancellationToken cancellationToken = default)
    {
        context.DiagramUploads.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DiagramUpload entity, CancellationToken cancellationToken = default)
    {
        context.DiagramUploads.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<DiagramUpload?> GetByFileHashAsync(string fileHash, string? userId, CancellationToken cancellationToken = default)
    {
        return await context.DiagramUploads
            .FirstOrDefaultAsync(d => d.FileHash.Value == fileHash && d.UserId == userId, cancellationToken);
    }

    public async Task<(IReadOnlyList<DiagramUpload> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, string? userId = null, CancellationToken cancellationToken = default)
    {
        var query = context.DiagramUploads.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(d => d.UserId == userId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<DiagramUpload>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await context.DiagramUploads
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
