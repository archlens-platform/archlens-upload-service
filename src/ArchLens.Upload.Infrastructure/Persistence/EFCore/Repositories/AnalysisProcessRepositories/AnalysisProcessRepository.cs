using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;
using ArchLens.Upload.Domain.Interfaces.AnalysisProcessInterfaces;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Upload.Infrastructure.Persistence.EFCore.Repositories.AnalysisProcessRepositories;

public sealed class AnalysisProcessRepository(UploadDbContext context) : IAnalysisProcessRepository
{
    public async Task<AnalysisProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.AnalysisProcesses.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(AnalysisProcess entity, CancellationToken cancellationToken = default)
    {
        await context.AnalysisProcesses.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(AnalysisProcess entity, CancellationToken cancellationToken = default)
    {
        context.AnalysisProcesses.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AnalysisProcess entity, CancellationToken cancellationToken = default)
    {
        context.AnalysisProcesses.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<AnalysisProcess?> GetByDiagramIdAsync(Guid diagramId, CancellationToken cancellationToken = default)
    {
        return await context.AnalysisProcesses
            .FirstOrDefaultAsync(a => a.DiagramUploadId == diagramId, cancellationToken);
    }
}
