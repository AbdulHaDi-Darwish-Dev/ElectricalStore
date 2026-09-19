using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricalStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameMediaStorageKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PublicId",
                table: "ProductImages",
                newName: "StorageKey");

            migrationBuilder.RenameColumn(
                name: "ImagePublicId",
                table: "Categories",
                newName: "ImageStorageKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StorageKey",
                table: "ProductImages",
                newName: "PublicId");

            migrationBuilder.RenameColumn(
                name: "ImageStorageKey",
                table: "Categories",
                newName: "ImagePublicId");
        }
    }
}
