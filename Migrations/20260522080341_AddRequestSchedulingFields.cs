using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestSchedulingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "Student_Schedule",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "Student_Schedule",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "Student_Schedule",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "class_date",
                table: "Request",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "day",
                table: "Request",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parent_request_id",
                table: "Request",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_type",
                table: "Request",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Request_parent_request_id",
                table: "Request",
                column: "parent_request_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Request_ParentRequest",
                table: "Request",
                column: "parent_request_id",
                principalTable: "Request",
                principalColumn: "request_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Request_ParentRequest",
                table: "Request");

            migrationBuilder.DropIndex(
                name: "IX_Request_parent_request_id",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "class_date",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "day",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "parent_request_id",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "request_type",
                table: "Request");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "Student_Schedule",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "Student_Schedule",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "Student_Schedule",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);
        }
    }
}
