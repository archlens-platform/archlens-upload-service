using System;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchLens.Upload.Infrastructure.Persistence.EFCore.Migrations
{
    [DbContext(typeof(UploadDbContext))]
    [Migration("20260310003115_InitialCreate")]
    partial class InitialCreate
    {

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ArchLens.Upload.Domain.Entities.AnalysisProcess", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("DiagramUploadId")
                        .HasColumnType("uuid");

                    b.Property<string>("ErrorMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.HasKey("Id");

                    b.HasIndex("DiagramUploadId");

                    b.ToTable("analysis_processes", (string)null);
                });

            modelBuilder.Entity("ArchLens.Upload.Domain.Entities.DiagramUpload", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<long>("FileSize")
                        .HasColumnType("bigint");

                    b.Property<string>("FileType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("StoragePath")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("UserId")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt")
                        .IsDescending();

                    b.HasIndex("UserId");

                    b.ToTable("diagram_uploads", (string)null);
                });

            modelBuilder.Entity("ArchLens.Upload.Infrastructure.Persistence.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Error")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<DateTime?>("ProcessedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("RetryCount")
                        .HasColumnType("integer");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("ProcessedAt", "CreatedAt")
                        .HasFilter("\"ProcessedAt\" IS NULL");

                    b.ToTable("outbox_messages", (string)null);
                });

            modelBuilder.Entity("ArchLens.Upload.Domain.Entities.DiagramUpload", b =>
                {
                    b.OwnsOne("ArchLens.Upload.Domain.ValueObjects.DiagramStatus", "Status", b1 =>
                        {
                            b1.Property<Guid>("DiagramUploadId")
                                .HasColumnType("uuid");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(20)
                                .HasColumnType("character varying(20)")
                                .HasColumnName("status");

                            b1.HasKey("DiagramUploadId");

                            b1.ToTable("diagram_uploads");

                            b1.WithOwner()
                                .HasForeignKey("DiagramUploadId");
                        });

                    b.OwnsOne("ArchLens.Upload.Domain.ValueObjects.FileHash", "FileHash", b1 =>
                        {
                            b1.Property<Guid>("DiagramUploadId")
                                .HasColumnType("uuid");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("file_hash");

                            b1.HasKey("DiagramUploadId");

                            b1.HasIndex("Value")
                                .IsUnique();

                            b1.ToTable("diagram_uploads");

                            b1.WithOwner()
                                .HasForeignKey("DiagramUploadId");
                        });

                    b.Navigation("FileHash")
                        .IsRequired();

                    b.Navigation("Status")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
