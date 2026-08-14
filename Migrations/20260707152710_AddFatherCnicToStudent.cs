using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseofTutorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFatherCnicToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FatherCnic",
                table: "Student",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FatherCnic",
                table: "Student");
        }
    }
}
