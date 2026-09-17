using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PricingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeatType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HallType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DayType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeSlot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRules", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 6, 26, 32, 420, DateTimeKind.Utc).AddTicks(5001));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 6, 26, 32, 420, DateTimeKind.Utc).AddTicks(5004));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 6, 26, 32, 420, DateTimeKind.Utc).AddTicks(4931));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 6, 26, 32, 420, DateTimeKind.Utc).AddTicks(4943));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 6, 26, 32, 420, DateTimeKind.Utc).AddTicks(4944));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingRules");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 5, 35, 9, 426, DateTimeKind.Utc).AddTicks(334));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 5, 35, 9, 426, DateTimeKind.Utc).AddTicks(337));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 5, 35, 9, 426, DateTimeKind.Utc).AddTicks(281));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 5, 35, 9, 426, DateTimeKind.Utc).AddTicks(286));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 5, 35, 9, 426, DateTimeKind.Utc).AddTicks(287));
        }
    }
}
