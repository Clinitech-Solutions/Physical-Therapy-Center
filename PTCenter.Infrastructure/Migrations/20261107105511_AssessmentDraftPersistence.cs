using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentDraftPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftJson",
                table: "Assessments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftJson",
                table: "Assessments");
        }
    }
}
