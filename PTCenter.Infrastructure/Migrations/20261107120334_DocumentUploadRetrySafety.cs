using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentUploadRetrySafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadBatchHash",
                table: "InsuranceDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadIndex",
                table: "InsuranceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadOperationKey",
                table: "InsuranceDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceDocuments_UploadOperationKey_UploadIndex",
                table: "InsuranceDocuments",
                columns: new[] { "UploadOperationKey", "UploadIndex" },
                unique: true,
                filter: "[UploadOperationKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InsuranceDocuments_UploadOperationKey_UploadIndex",
                table: "InsuranceDocuments");

            migrationBuilder.DropColumn(
                name: "UploadBatchHash",
                table: "InsuranceDocuments");

            migrationBuilder.DropColumn(
                name: "UploadIndex",
                table: "InsuranceDocuments");

            migrationBuilder.DropColumn(
                name: "UploadOperationKey",
                table: "InsuranceDocuments");
        }
    }
}
