using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentCourseKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Course",
                columns: table => new
                {
                    course_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    course_title = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Course__8F1EF7AEB9F1622F", x => x.course_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    full_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    cnic = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    password = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__B9BE370FAAD384A7", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    student_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    location = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student__2A33069A6C1D77D6", x => x.student_id);
                    table.ForeignKey(
                        name: "FK__Student__user_id__5165187F",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Tutor",
                columns: table => new
                {
                    tutor_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    qualification = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    experience = table.Column<int>(type: "int", nullable: true),
                    location = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    radius = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tutor__50DE5D0142BF8F72", x => x.tutor_id);
                    table.ForeignKey(
                        name: "FK__Tutor__user_id__4E88ABD4",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Student_Course",
                columns: table => new
                {
                    student_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Course", x => new { x.student_id, x.course_id });
                    table.ForeignKey(
                        name: "FK_Student_Course_Course_course_id",
                        column: x => x.course_id,
                        principalTable: "Course",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Student_Course_Student_student_id",
                        column: x => x.student_id,
                        principalTable: "Student",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Student_Schedule",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    day = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    time = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Schedule", x => x.schedule_id);
                    table.ForeignKey(
                        name: "FK_StudentSchedule_Student",
                        column: x => x.student_id,
                        principalTable: "Student",
                        principalColumn: "student_id");
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    feedback_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    tutor_id = table.Column<int>(type: "int", nullable: true),
                    course_id = table.Column<int>(type: "int", nullable: true),
                    rating = table.Column<int>(type: "int", nullable: true),
                    comment = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    feedback_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Feedback__7A6B2B8C35A2CDED", x => x.feedback_id);
                    table.ForeignKey(
                        name: "FK__Feedback__course__66603565",
                        column: x => x.course_id,
                        principalTable: "Course",
                        principalColumn: "course_id");
                    table.ForeignKey(
                        name: "FK__Feedback__studen__6477ECF3",
                        column: x => x.student_id,
                        principalTable: "Student",
                        principalColumn: "student_id");
                    table.ForeignKey(
                        name: "FK__Feedback__tutor___656C112C",
                        column: x => x.tutor_id,
                        principalTable: "Tutor",
                        principalColumn: "tutor_id");
                });

            migrationBuilder.CreateTable(
                name: "Request",
                columns: table => new
                {
                    request_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    tutor_id = table.Column<int>(type: "int", nullable: true),
                    course_id = table.Column<int>(type: "int", nullable: true),
                    request_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Request__18D3B90F519F4A7A", x => x.request_id);
                    table.ForeignKey(
                        name: "FK__Request__course___5FB337D6",
                        column: x => x.course_id,
                        principalTable: "Course",
                        principalColumn: "course_id");
                    table.ForeignKey(
                        name: "FK__Request__student__5DCAEF64",
                        column: x => x.student_id,
                        principalTable: "Student",
                        principalColumn: "student_id");
                    table.ForeignKey(
                        name: "FK__Request__tutor_i__5EBF139D",
                        column: x => x.tutor_id,
                        principalTable: "Tutor",
                        principalColumn: "tutor_id");
                });

            migrationBuilder.CreateTable(
                name: "Schedule",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tutor_id = table.Column<int>(type: "int", nullable: true),
                    day = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    time = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Schedule__C46A8A6FEF990444", x => x.schedule_id);
                    table.ForeignKey(
                        name: "FK__Schedule__tutor___5629CD9C",
                        column: x => x.tutor_id,
                        principalTable: "Tutor",
                        principalColumn: "tutor_id");
                });

            migrationBuilder.CreateTable(
                name: "Tutor_Course",
                columns: table => new
                {
                    tutor_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    grade = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tutor_Co__A82FB27BA4857372", x => new { x.tutor_id, x.course_id });
                    table.ForeignKey(
                        name: "FK__Tutor_Cou__cours__59FA5E80",
                        column: x => x.course_id,
                        principalTable: "Course",
                        principalColumn: "course_id");
                    table.ForeignKey(
                        name: "FK__Tutor_Cou__tutor__59063A47",
                        column: x => x.tutor_id,
                        principalTable: "Tutor",
                        principalColumn: "tutor_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_course_id",
                table: "Feedback",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_student_id",
                table: "Feedback",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_tutor_id",
                table: "Feedback",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "IX_Request_course_id",
                table: "Request",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_Request_student_id",
                table: "Request",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Request_tutor_id",
                table: "Request",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "IX_Schedule_tutor_id",
                table: "Schedule",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_user_id",
                table: "Student",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Course_course_id",
                table: "Student_Course",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Schedule_student_id",
                table: "Student_Schedule",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_user_id",
                table: "Tutor",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Course_course_id",
                table: "Tutor_Course",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__AB6E6164B2DCE846",
                table: "Users",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "Request");

            migrationBuilder.DropTable(
                name: "Schedule");

            migrationBuilder.DropTable(
                name: "Student_Course");

            migrationBuilder.DropTable(
                name: "Student_Schedule");

            migrationBuilder.DropTable(
                name: "Tutor_Course");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "Course");

            migrationBuilder.DropTable(
                name: "Tutor");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
