using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricalStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectGuestIdempotencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Discard prior plaintext guest tokens — cannot be re-protected offline.
            migrationBuilder.Sql("DELETE FROM [OrderPlacementIdempotencies];");

            migrationBuilder.DropColumn(
                name: "GuestAccessToken",
                table: "OrderPlacementIdempotencies");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "OrderPlacementIdempotencies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ProtectedGuestAccessToken",
                table: "OrderPlacementIdempotencies",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPlacementIdempotencies_ExpiresAtUtc",
                table: "OrderPlacementIdempotencies",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderPlacementIdempotencies_ExpiresAtUtc",
                table: "OrderPlacementIdempotencies");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "OrderPlacementIdempotencies");

            migrationBuilder.DropColumn(
                name: "ProtectedGuestAccessToken",
                table: "OrderPlacementIdempotencies");

            migrationBuilder.AddColumn<string>(
                name: "GuestAccessToken",
                table: "OrderPlacementIdempotencies",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
