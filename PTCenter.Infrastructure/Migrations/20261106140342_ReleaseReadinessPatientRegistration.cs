using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseReadinessPatientRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegistrationOperationKey",
                table: "Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationRequestHash",
                table: "Patients",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_RegistrationOperationKey",
                table: "Patients",
                column: "RegistrationOperationKey",
                unique: true,
                filter: "[RegistrationOperationKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_RegistrationOperationKey",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "RegistrationOperationKey",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "RegistrationRequestHash",
                table: "Patients");
        }
    }
}
