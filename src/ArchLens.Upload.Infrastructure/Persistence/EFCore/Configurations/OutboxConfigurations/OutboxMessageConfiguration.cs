using ArchLens.Upload.Infrastructure.Persistence.EFCore.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchLens.Upload.Infrastructure.Persistence.EFCore.Configurations.OutboxConfigurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);

        builder.HasIndex(x => new { x.ProcessedAt, x.CreatedAt })
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}
