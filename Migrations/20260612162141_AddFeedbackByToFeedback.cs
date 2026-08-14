using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackByToFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "feedback_by",
                table: "Feedback",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "feedback_by",
                table: "Feedback");
        }
    }
}
