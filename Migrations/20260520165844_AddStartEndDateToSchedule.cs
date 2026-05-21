using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStartEndDateToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Student_Course_Course_course_id",
                table: "Student_Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_Course_Student_student_id",
                table: "Student_Course");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Schedule",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Schedule",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourse_Course",
                table: "Student_Course",
                column: "course_id",
                principalTable: "Course",
                principalColumn: "course_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourse_Student",
                table: "Student_Course",
                column: "student_id",
                principalTable: "Student",
                principalColumn: "student_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourse_Course",
                table: "Student_Course");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourse_Student",
                table: "Student_Course");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Schedule");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Course_Course_course_id",
                table: "Student_Course",
                column: "course_id",
                principalTable: "Course",
                principalColumn: "course_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_Course_Student_student_id",
                table: "Student_Course",
                column: "student_id",
                principalTable: "Student",
                principalColumn: "student_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
