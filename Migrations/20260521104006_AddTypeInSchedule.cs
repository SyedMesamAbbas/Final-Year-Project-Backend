using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeInSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Schedule",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Schedule",
                newName: "end_date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "Schedule",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "Schedule",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Schedule");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "Schedule",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "end_date",
                table: "Schedule",
                newName: "EndDate");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Schedule",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Schedule",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);
        }
    }
}
