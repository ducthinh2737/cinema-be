using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                columns: new[] { "CinemaName", "CreatedAt" },
                values: new object[] { "Cinema 1", new DateTime(2026, 6, 14, 7, 28, 27, 30, DateTimeKind.Utc).AddTicks(4626) });

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                columns: new[] { "CinemaName", "CreatedAt" },
                values: new object[] { "Cinema 2", new DateTime(2026, 6, 14, 7, 28, 27, 30, DateTimeKind.Utc).AddTicks(4629) });

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 7, 28, 27, 30, DateTimeKind.Utc).AddTicks(4544));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 7, 28, 27, 30, DateTimeKind.Utc).AddTicks(4550));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 7, 28, 27, 30, DateTimeKind.Utc).AddTicks(4551));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Reviews");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                columns: new[] { "CinemaName", "CreatedAt" },
                values: new object[] { "Cinema Center District 1", new DateTime(2026, 6, 11, 4, 59, 37, 4, DateTimeKind.Utc).AddTicks(6502) });

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                columns: new[] { "CinemaName", "CreatedAt" },
                values: new object[] { "Cinema Center Dong Da", new DateTime(2026, 6, 11, 4, 59, 37, 4, DateTimeKind.Utc).AddTicks(6505) });

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 11, 4, 59, 37, 4, DateTimeKind.Utc).AddTicks(6445));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 11, 4, 59, 37, 4, DateTimeKind.Utc).AddTicks(6454));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 11, 4, 59, 37, 4, DateTimeKind.Utc).AddTicks(6455));
        }
    }
}
