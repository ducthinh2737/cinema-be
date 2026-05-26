using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaAuditingAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TypeName",
                table: "SeatTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SeatTypes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SeatTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SeatTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SeatTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SeatTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "SeatTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "SeatTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SeatCode",
                table: "Seats",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Seats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Seats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Seats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Seats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Seats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "Seats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Seats",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TypeName",
                table: "HallTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "HallTypes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "HallTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "HallTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "HallTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "HallTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "HallTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "HallTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HallName",
                table: "Halls",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Halls",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Halls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Halls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "Halls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CinemaName",
                table: "Cinemas",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Cinemas",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Cinemas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Cinemas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Cinemas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Cinemas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "Cinemas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ImageUrl", "IsDeleted", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTime(2026, 5, 26, 11, 37, 42, 442, DateTimeKind.Utc).AddTicks(64), null, null, null, null, false, null, null });

            migrationBuilder.UpdateData(
                table: "Cinemas",
                keyColumn: "CinemaId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ImageUrl", "IsDeleted", "LastModifiedAt", "LastModifiedBy" },
                values: new object[] { new DateTime(2026, 5, 26, 11, 37, 42, 442, DateTimeKind.Utc).AddTicks(67), null, null, null, null, false, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SeatTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SeatTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SeatTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SeatTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SeatTypes");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "SeatTypes");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "SeatTypes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "HallTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "HallTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "HallTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "HallTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "HallTypes");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "HallTypes");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "HallTypes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Halls");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Cinemas");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Cinemas");

            migrationBuilder.AlterColumn<string>(
                name: "TypeName",
                table: "SeatTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SeatCode",
                table: "Seats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "TypeName",
                table: "HallTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "HallName",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CinemaName",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Cinemas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);
        }
    }
}
