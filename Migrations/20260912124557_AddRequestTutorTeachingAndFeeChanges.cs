using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestTutorTeachingAndFeeChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "institute",
                table: "Tutor_Course",
                type: "varchar(150)",
                unicode: false,
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "teaching_mode",
                table: "Tutor",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fee_responsibility",
                table: "Student_Course_Fee",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Parent");

            migrationBuilder.AddColumn<int>(
                name: "request_group_id",
                table: "Request",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "response_deadline",
                table: "Request",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tutor_sequence",
                table: "Request",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "admin_set_max_hourly_rate",
                table: "Course",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "admin_set_min_hourly_rate",
                table: "Course",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Request_Group",
                columns: table => new
                {
                    request_group_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    created_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    current_request_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Request___30D47C2981974292", x => x.request_group_id);
                    table.ForeignKey(
                        name: "FK__Request_G__cours__51300E55",
                        column: x => x.course_id,
                        principalTable: "Course",
                        principalColumn: "course_id");
                    table.ForeignKey(
                        name: "FK__Request_G__stude__503BEA1C",
                        column: x => x.student_id,
                        principalTable: "Student",
                        principalColumn: "student_id");
                });

            migrationBuilder.CreateTable(
                name: "Student_Course_Content",
                columns: table => new
                {
                    content_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    file_name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    file_path = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    uploaded_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student___655FE5105D0E97CF", x => x.content_id);
                    table.ForeignKey(
                        name: "FK__Student_C__cours__56E8E7AB",
                        column: x => x.course_id,
                        principalTable: "Course",
                        principalColumn: "course_id");
                    table.ForeignKey(
                        name: "FK__Student_C__stude__55F4C372",
                        column: x => x.student_id,
                        principalTable: "Student",
                        principalColumn: "student_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Request_request_group_id",
                table: "Request",
                column: "request_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_Request_Group_course_id",
                table: "Request_Group",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_Request_Group_student_id",
                table: "Request_Group",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Course_Content_course_id",
                table: "Student_Course_Content",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Course_Content_student_id",
                table: "Student_Course_Content",
                column: "student_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Request_RequestGroup",
                table: "Request",
                column: "request_group_id",
                principalTable: "Request_Group",
                principalColumn: "request_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Request_RequestGroup",
                table: "Request");

            migrationBuilder.DropTable(
                name: "Request_Group");

            migrationBuilder.DropTable(
                name: "Student_Course_Content");

            migrationBuilder.DropIndex(
                name: "IX_Request_request_group_id",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "institute",
                table: "Tutor_Course");

            migrationBuilder.DropColumn(
                name: "teaching_mode",
                table: "Tutor");

            migrationBuilder.DropColumn(
                name: "fee_responsibility",
                table: "Student_Course_Fee");

            migrationBuilder.DropColumn(
                name: "request_group_id",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "response_deadline",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "tutor_sequence",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "admin_set_max_hourly_rate",
                table: "Course");

            migrationBuilder.DropColumn(
                name: "admin_set_min_hourly_rate",
                table: "Course");
        }
    }
}
