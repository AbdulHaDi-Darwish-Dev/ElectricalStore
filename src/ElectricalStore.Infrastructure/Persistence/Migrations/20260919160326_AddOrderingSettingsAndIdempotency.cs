using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricalStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderingSettingsAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinimumMerchandiseSubtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderPlacementIdempotencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestAccessToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPlacementIdempotencies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPlacementIdempotencies_OrderId",
                table: "OrderPlacementIdempotencies",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPlacementIdempotencies_Scope_KeyHash",
                table: "OrderPlacementIdempotencies",
                columns: new[] { "Scope", "KeyHash" },
                unique: true);

            migrationBuilder.InsertData(
                table: "OrderingSettings",
                columns: new[] { "Id", "MinimumMerchandiseSubtotal" },
                values: new object[] { new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001"), 0m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderingSettings");

            migrationBuilder.DropTable(
                name: "OrderPlacementIdempotencies");
        }
    }
}
