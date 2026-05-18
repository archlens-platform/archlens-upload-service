using ArchLens.Upload.Domain.Entities.DiagramUploadEntities;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchLens.Upload.Infrastructure.Persistence.EFCore.Configurations.DiagramUploadConfigurations;

public sealed class DiagramUploadConfiguration : IEntityTypeConfiguration<DiagramUpload>
{
    public void Configure(EntityTypeBuilder<DiagramUpload> builder)
    {
        builder.ToTable("diagram_uploads");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FileType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FileSize).IsRequired();

        builder.OwnsOne(x => x.FileHash, fh =>
        {
            fh.Property(v => v.Value)
                .HasColumnName("file_hash")
                .HasMaxLength(64)
                .IsRequired();

            fh.HasIndex(v => v.Value);
        });

        builder.OwnsOne(x => x.Status, s =>
        {
            s.Property(v => v.Value)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(x => x.StoragePath).HasMaxLength(512).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(128);

        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => x.CreatedAt).IsDescending();
        builder.HasIndex(x => x.UserId);
    }
}
