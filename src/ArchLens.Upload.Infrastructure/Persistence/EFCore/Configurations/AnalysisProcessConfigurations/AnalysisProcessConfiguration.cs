using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchLens.Upload.Infrastructure.Persistence.EFCore.Configurations.AnalysisProcessConfigurations;

public sealed class AnalysisProcessConfiguration : IEntityTypeConfiguration<AnalysisProcess>
{
    public void Configure(EntityTypeBuilder<AnalysisProcess> builder)
    {
        builder.ToTable("analysis_processes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.DiagramUploadId).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.CompletedAt);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => x.DiagramUploadId);
    }
}
