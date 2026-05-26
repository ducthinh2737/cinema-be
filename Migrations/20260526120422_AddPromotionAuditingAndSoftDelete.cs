using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionAuditingAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PromoCode",
                table: "Promotions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "CurrentUsage",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Promotions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "Promotions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Promotions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsage",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Promotions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 12, 4, 22, 153, DateTimeKind.Utc).AddTicks(7835));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 12, 4, 22, 153, DateTimeKind.Utc).AddTicks(7839));

            migrationBuilder.CreateIndex(
                name: "IX_BookingPromotions_BookingId",
                table: "BookingPromotions",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPromotions_PromotionId",
                table: "BookingPromotions",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingPromotions_Bookings_BookingId",
                table: "BookingPromotions",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingPromotions_Promotions_PromotionId",
                table: "BookingPromotions",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "PromotionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingPromotions_Bookings_BookingId",
                table: "BookingPromotions");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingPromotions_Promotions_PromotionId",
                table: "BookingPromotions");

            migrationBuilder.DropIndex(
                name: "IX_BookingPromotions_BookingId",
                table: "BookingPromotions");

            migrationBuilder.DropIndex(
                name: "IX_BookingPromotions_PromotionId",
                table: "BookingPromotions");

            migrationBuilder.DropColumn(
                name: "CurrentUsage",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaxUsage",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Promotions");

            migrationBuilder.AlterColumn<string>(
                name: "PromoCode",
                table: "Promotions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 12, 0, 25, 251, DateTimeKind.Utc).AddTicks(5250));

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 12, 0, 25, 251, DateTimeKind.Utc).AddTicks(5253));
        }
    }
}
