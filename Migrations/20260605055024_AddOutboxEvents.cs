using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 5, 50, 22, 941, DateTimeKind.Utc).AddTicks(439));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 5, 50, 22, 941, DateTimeKind.Utc).AddTicks(444));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 5, 50, 22, 941, DateTimeKind.Utc).AddTicks(338));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 5, 50, 22, 941, DateTimeKind.Utc).AddTicks(347));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 5, 50, 22, 941, DateTimeKind.Utc).AddTicks(349));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 8, 4, 20, 352, DateTimeKind.Utc).AddTicks(8069));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 8, 4, 20, 352, DateTimeKind.Utc).AddTicks(8073));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 8, 4, 20, 352, DateTimeKind.Utc).AddTicks(7940));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 8, 4, 20, 352, DateTimeKind.Utc).AddTicks(7948));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 1, 8, 4, 20, 352, DateTimeKind.Utc).AddTicks(7950));
        }
    }
}
