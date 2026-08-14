using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Request_ParentRequest",
                table: "Request");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourse_Course",
                table: "Student_Course");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourse_Student",
                table: "Student_Course");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSchedule_Student",
                table: "Student_Schedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Student_Schedule",
                table: "Student_Schedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Student_Course",
                table: "Student_Course");

            migrationBuilder.DropIndex(
                name: "IX_Request_parent_request_id",
                table: "Request");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Student_Schedule",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "FatherCnic",
                table: "Student",
                newName: "Father_Cnic");

            migrationBuilder.AlterColumn<bool>(
                name: "is_completed",
                table: "Tutor_Course",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "Tutor",
                type: "varchar(max)",
                unicode: false,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "time",
                table: "Student_Schedule",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grade",
                table: "Student_Course",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "Student",
                type: "varchar(max)",
                unicode: false,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Father_Cnic",
                table: "Student",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Student",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "time",
                table: "Schedule",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "start_date",
                table: "Schedule",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "end_date",
                table: "Schedule",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Schedule",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Time",
                table: "Request",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Request",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "learning_duration",
                table: "Request",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "learning_duration_unit",
                table: "Request",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "learning_mode",
                table: "Request",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "feedback_by",
                table: "Feedback",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20);

            migrationBuilder.AddPrimaryKey(
                name: "PK__Student___C46A8A6FA77E8ECE",
                table: "Student_Schedule",
                column: "schedule_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Student___D2C2E9E09DC4D622",
                table: "Student_Course",
                columns: new[] { "student_id", "course_id" });

            migrationBuilder.CreateTable(
                name: "Student_Course_Fee",
                columns: table => new
                {
                    fee_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    tutor_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    total_fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    created_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student___A19C8AFB7090B923", x => x.fee_id);
                    table.ForeignKey(
                        name: "FK__Student_C__cours__22751F6C",
                        column: x => x.course_id,
                        principalTable: "Course",
                        principalColumn: "course_id");
                    table.ForeignKey(
                        name: "FK__Student_C__stude__208CD6FA",
                        column: x => x.student_id,
                        principalTable: "Student",
                        principalColumn: "student_id");
                    table.ForeignKey(
                        name: "FK__Student_C__tutor__2180FB33",
                        column: x => x.tutor_id,
                        principalTable: "Tutor",
                        principalColumn: "tutor_id");
                });

            migrationBuilder.CreateTable(
                name: "Tutor_Course_Rate",
                columns: table => new
                {
                    rate_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tutor_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    hourly_rate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    admin_set_min_hourly_rate = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    admin_set_max_hourly_rate = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tutor_Co__75920B42B74529C1", x => x.rate_id);
                    table.ForeignKey(
                        name: "FK__Tutor_Cou__cours__1BC821DD",
                        column: x => x.course_id,
                        principalTable: "Course",
                        principalColumn: "course_id");
                    table.ForeignKey(
                        name: "FK__Tutor_Cou__tutor__1AD3FDA4",
                        column: x => x.tutor_id,
                        principalTable: "Tutor",
                        principalColumn: "tutor_id");
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fee_id = table.Column<int>(type: "int", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    payment_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    payment_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    parent_status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    tutor_status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    remarks = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payment__ED1FC9EA8811CF21", x => x.payment_id);
                    table.ForeignKey(
                        name: "FK__Payment__fee_id__2645B050",
                        column: x => x.fee_id,
                        principalTable: "Student_Course_Fee",
                        principalColumn: "fee_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_fee_id",
                table: "Payment",
                column: "fee_id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Course_Fee_course_id",
                table: "Student_Course_Fee",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Course_Fee_tutor_id",
                table: "Student_Course_Fee",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Student___80B1FDBC799B4612",
                table: "Student_Course_Fee",
                columns: new[] { "student_id", "tutor_id", "course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Course_Rate_course_id",
                table: "Tutor_Course_Rate",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Course_Rate_tutor_id",
                table: "Tutor_Course_Rate",
                column: "tutor_id");

            migrationBuilder.AddForeignKey(
                name: "FK__Student_C__cours__72C60C4A",
                table: "Student_Course",
                column: "course_id",
                principalTable: "Course",
                principalColumn: "course_id");

            migrationBuilder.AddForeignKey(
                name: "FK__Student_S__stude__6D0D32F4",
                table: "Student_Schedule",
                column: "student_id",
                principalTable: "Student",
                principalColumn: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Student_C__cours__72C60C4A",
                table: "Student_Course");

            migrationBuilder.DropForeignKey(
                name: "FK__Student_S__stude__6D0D32F4",
                table: "Student_Schedule");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "Tutor_Course_Rate");

            migrationBuilder.DropTable(
                name: "Student_Course_Fee");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Student___C46A8A6FA77E8ECE",
                table: "Student_Schedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Student___D2C2E9E09DC4D622",
                table: "Student_Course");

            migrationBuilder.DropColumn(
                name: "grade",
                table: "Student_Course");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "learning_duration",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "learning_duration_unit",
                table: "Request");

            migrationBuilder.DropColumn(
                name: "learning_mode",
                table: "Request");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Student_Schedule",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Father_Cnic",
                table: "Student",
                newName: "FatherCnic");

            migrationBuilder.AlterColumn<bool>(
                name: "is_completed",
                table: "Tutor_Course",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "Tutor",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldUnicode: false,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "time",
                table: "Student_Schedule",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "Student",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldUnicode: false,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FatherCnic",
                table: "Student",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "time",
                table: "Schedule",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "Schedule",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "Schedule",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Schedule",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Time",
                table: "Request",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Request",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "feedback_by",
                table: "Feedback",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Student_Schedule",
                table: "Student_Schedule",
                column: "schedule_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Student_Course",
                table: "Student_Course",
                columns: new[] { "student_id", "course_id" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSchedule_Student",
                table: "Student_Schedule",
                column: "student_id",
                principalTable: "Student",
                principalColumn: "student_id");
        }
    }
}
