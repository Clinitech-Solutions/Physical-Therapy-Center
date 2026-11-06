using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InsuranceChequeReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InsuranceChequeReceiptId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InsuranceServiceDeliveredAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InsuranceServiceMonth",
                table: "Payments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InsurerBalanceBefore",
                table: "Payments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionBatchId",
                table: "InsuranceClaims",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InsuranceChequeReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceCompanyId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChequeIdentityHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChequeNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ChequeDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RecordedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceChequeReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceChequeReceipts_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceClaimSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceCompanyId = table.Column<int>(type: "int", nullable: false),
                    ServiceMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaimSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceClaimSubmissions_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InsuranceChequeReceiptId_InsuranceClaimId",
                table: "Payments",
                columns: new[] { "InsuranceChequeReceiptId", "InsuranceClaimId" },
                unique: true,
                filter: "[InsuranceChequeReceiptId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_SubmissionBatchId",
                table: "InsuranceClaims",
                column: "SubmissionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceChequeReceipts_ChequeIdentityHash",
                table: "InsuranceChequeReceipts",
                column: "ChequeIdentityHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceChequeReceipts_IdempotencyKey",
                table: "InsuranceChequeReceipts",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceChequeReceipts_InsuranceCompanyId_ReceivedOn",
                table: "InsuranceChequeReceipts",
                columns: new[] { "InsuranceCompanyId", "ReceivedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaimSubmissions_IdempotencyKey",
                table: "InsuranceClaimSubmissions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaimSubmissions_InsuranceCompanyId_ServiceMonth",
                table: "InsuranceClaimSubmissions",
                columns: new[] { "InsuranceCompanyId", "ServiceMonth" });

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceClaims_InsuranceClaimSubmissions_SubmissionBatchId",
                table: "InsuranceClaims",
                column: "SubmissionBatchId",
                principalTable: "InsuranceClaimSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_InsuranceChequeReceipts_InsuranceChequeReceiptId",
                table: "Payments",
                column: "InsuranceChequeReceiptId",
                principalTable: "InsuranceChequeReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceClaims_InsuranceClaimSubmissions_SubmissionBatchId",
                table: "InsuranceClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_InsuranceChequeReceipts_InsuranceChequeReceiptId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "InsuranceChequeReceipts");

            migrationBuilder.DropTable(
                name: "InsuranceClaimSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_InsuranceChequeReceiptId_InsuranceClaimId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceClaims_SubmissionBatchId",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "InsuranceChequeReceiptId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InsuranceServiceDeliveredAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InsuranceServiceMonth",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InsurerBalanceBefore",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubmissionBatchId",
                table: "InsuranceClaims");
        }
    }
}
