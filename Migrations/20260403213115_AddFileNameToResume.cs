using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeTailorApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFileNameToResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Resumes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Resumes");
        }
    }
}
