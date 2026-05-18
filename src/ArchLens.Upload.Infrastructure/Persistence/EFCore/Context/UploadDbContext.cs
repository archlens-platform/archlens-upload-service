using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;
using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Configurations.AnalysisProcessConfigurations;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Configurations.DiagramUploadConfigurations;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Configurations.OutboxConfigurations;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;

public sealed class UploadDbContext(DbContextOptions<UploadDbContext> options) : DbContext(options)
{
    public DbSet<DiagramUpload> DiagramUploads => Set<DiagramUpload>();
    public DbSet<AnalysisProcess> AnalysisProcesses => Set<AnalysisProcess>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DiagramUploadConfiguration());
        modelBuilder.ApplyConfiguration(new AnalysisProcessConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
