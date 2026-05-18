using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchLens.Upload.Infrastructure.Persistence.EFCore.Migrations;

/// <inheritdoc />
public partial class UserScopedHashDedup : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the global unique index on file_hash. Hash dedup is now scoped per user
        // so two users CAN upload the same file (each gets their own analysis namespace).
        migrationBuilder.DropIndex(
            name: "IX_diagram_uploads_file_hash",
            table: "diagram_uploads");

        // Replace with a non-unique index for lookup performance.
        migrationBuilder.CreateIndex(
            name: "IX_diagram_uploads_file_hash",
            table: "diagram_uploads",
            column: "file_hash");

        // Composite unique index (UserId, file_hash) enforces per-user dedup at DB level.
        // NULL UserIds are tolerated and not deduped (acceptable for anonymous uploads).
        migrationBuilder.CreateIndex(
            name: "IX_diagram_uploads_user_file_hash",
            table: "diagram_uploads",
            columns: new[] { "UserId", "file_hash" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_diagram_uploads_user_file_hash",
            table: "diagram_uploads");

        migrationBuilder.DropIndex(
            name: "IX_diagram_uploads_file_hash",
            table: "diagram_uploads");

        migrationBuilder.CreateIndex(
            name: "IX_diagram_uploads_file_hash",
            table: "diagram_uploads",
            column: "file_hash",
            unique: true);
    }
}
