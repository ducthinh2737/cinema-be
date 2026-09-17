using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewStatusAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 8, 1, 10, 80, DateTimeKind.Utc).AddTicks(8179));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 8, 1, 10, 80, DateTimeKind.Utc).AddTicks(8197));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 8, 1, 10, 80, DateTimeKind.Utc).AddTicks(7591));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 8, 1, 10, 80, DateTimeKind.Utc).AddTicks(7628));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 8, 1, 10, 80, DateTimeKind.Utc).AddTicks(7629));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reviews");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 7, 53, 5, 855, DateTimeKind.Utc).AddTicks(539));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 7, 53, 5, 855, DateTimeKind.Utc).AddTicks(541));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 7, 53, 5, 855, DateTimeKind.Utc).AddTicks(481));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 7, 53, 5, 855, DateTimeKind.Utc).AddTicks(489));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 14, 7, 53, 5, 855, DateTimeKind.Utc).AddTicks(490));
        }
    }
}
