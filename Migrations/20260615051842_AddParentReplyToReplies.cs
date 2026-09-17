using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddParentReplyToReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentReplyId",
                table: "ReviewReplies",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 18, 41, 961, DateTimeKind.Utc).AddTicks(8139));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 18, 41, 961, DateTimeKind.Utc).AddTicks(8142));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 18, 41, 961, DateTimeKind.Utc).AddTicks(8077));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 18, 41, 961, DateTimeKind.Utc).AddTicks(8085));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 18, 41, 961, DateTimeKind.Utc).AddTicks(8086));

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReplies_ParentReplyId",
                table: "ReviewReplies",
                column: "ParentReplyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewReplies_ReviewReplies_ParentReplyId",
                table: "ReviewReplies",
                column: "ParentReplyId",
                principalTable: "ReviewReplies",
                principalColumn: "ReviewReplyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewReplies_ReviewReplies_ParentReplyId",
                table: "ReviewReplies");

            migrationBuilder.DropIndex(
                name: "IX_ReviewReplies_ParentReplyId",
                table: "ReviewReplies");

            migrationBuilder.DropColumn(
                name: "ParentReplyId",
                table: "ReviewReplies");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 7, 54, 103, DateTimeKind.Utc).AddTicks(3685));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 7, 54, 103, DateTimeKind.Utc).AddTicks(3688));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 7, 54, 103, DateTimeKind.Utc).AddTicks(3610));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 7, 54, 103, DateTimeKind.Utc).AddTicks(3616));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 15, 5, 7, 54, 103, DateTimeKind.Utc).AddTicks(3617));
        }
    }
}
