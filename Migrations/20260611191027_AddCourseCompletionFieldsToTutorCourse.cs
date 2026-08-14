using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCompletionFieldsToTutorCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "completed_date",
                table: "Tutor_Course",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_completed",
                table: "Tutor_Course",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completed_date",
                table: "Tutor_Course");

            migrationBuilder.DropColumn(
                name: "is_completed",
                table: "Tutor_Course");
        }
    }
}
