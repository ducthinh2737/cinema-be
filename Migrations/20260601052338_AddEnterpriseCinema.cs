using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseCinema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Halls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosingTime",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GalleryUrls",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleMapsUrl",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Cinemas",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Cinemas",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTime",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                columns: new[] { "BannerUrl", "ClosingTime", "CreatedAt", "Email", "GalleryUrls", "GoogleMapsUrl", "Latitude", "LogoUrl", "Longitude", "OpeningTime", "PhoneNumber", "Status" },
                values: new object[] { null, "23:00", new DateTime(2026, 6, 1, 5, 23, 38, 379, DateTimeKind.Utc).AddTicks(8805), null, null, null, null, null, null, "08:00", null, "Active" });

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                columns: new[] { "BannerUrl", "ClosingTime", "CreatedAt", "Email", "GalleryUrls", "GoogleMapsUrl", "Latitude", "LogoUrl", "Longitude", "OpeningTime", "PhoneNumber", "Status" },
                values: new object[] { null, "23:00", new DateTime(2026, 6, 1, 5, 23, 38, 379, DateTimeKind.Utc).AddTicks(8808), null, null, null, null, null, null, "08:00", null, "Active" });

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 5, 23, 38, 379, DateTimeKind.Utc).AddTicks(8753));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 5, 23, 38, 379, DateTimeKind.Utc).AddTicks(8761));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 5, 23, 38, 379, DateTimeKind.Utc).AddTicks(8762));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "GalleryUrls",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "GoogleMapsUrl",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Cinemas");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 31, 5, 42, 10, 357, DateTimeKind.Utc).AddTicks(2452));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 31, 5, 42, 10, 357, DateTimeKind.Utc).AddTicks(2455));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 31, 5, 42, 10, 357, DateTimeKind.Utc).AddTicks(2361));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 31, 5, 42, 10, 357, DateTimeKind.Utc).AddTicks(2405));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 31, 5, 42, 10, 357, DateTimeKind.Utc).AddTicks(2406));
        }
    }
}
