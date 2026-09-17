using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionsEnhanced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Promotions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Promotions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoApply",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "Promotions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumOrderValue",
                table: "Promotions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Promotions",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromotionConditions",
                columns: table => new
                {
                    PromotionConditionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionId = table.Column<int>(type: "int", nullable: false),
                    ApplyType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MovieId = table.Column<int>(type: "int", nullable: true),
                    ShowtimeId = table.Column<int>(type: "int", nullable: true),
                    MemberLevelId = table.Column<int>(type: "int", nullable: true),
                    StartHour = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndHour = table.Column<TimeSpan>(type: "time", nullable: true),
                    DaysOfWeek = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionConditions", x => x.PromotionConditionId);
                    table.ForeignKey(
                        name: "FK_PromotionConditions_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 5, 53, 11, 41, DateTimeKind.Utc).AddTicks(2852));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 5, 53, 11, 41, DateTimeKind.Utc).AddTicks(2855));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 5, 53, 11, 41, DateTimeKind.Utc).AddTicks(2793));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 5, 53, 11, 41, DateTimeKind.Utc).AddTicks(2798));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 5, 53, 11, 41, DateTimeKind.Utc).AddTicks(2799));

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PromotionId",
                table: "Bookings",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionConditions_PromotionId",
                table: "PromotionConditions",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Promotions_PromotionId",
                table: "Bookings",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "PromotionId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Promotions_PromotionId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "PromotionConditions");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PromotionId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsAutoApply",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MinimumOrderValue",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Bookings");

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 6, 12, 41, 200, DateTimeKind.Utc).AddTicks(2728));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 6, 12, 41, 200, DateTimeKind.Utc).AddTicks(2733));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 6, 12, 41, 200, DateTimeKind.Utc).AddTicks(2674));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 6, 12, 41, 200, DateTimeKind.Utc).AddTicks(2681));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 17, 6, 12, 41, 200, DateTimeKind.Utc).AddTicks(2682));
        }
    }
}
