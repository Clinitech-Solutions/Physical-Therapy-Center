using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CourseFinancialAgreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectionMethod",
                table: "Payments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsuranceClaimId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayerType",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AppointmentId",
                table: "Invoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CourseAgreementId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TreatmentPlanSessionId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseAgreementId",
                table: "BookingBatches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TreatmentCourseAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    TreatmentPlanId = table.Column<int>(type: "int", nullable: false),
                    FundingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PricingMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrescribedSessions = table.Column<int>(type: "int", nullable: false),
                    CoveredSessions = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PatientAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InsuranceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CopayPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InsuranceCompanyId = table.Column<int>(type: "int", nullable: true),
                    ApprovedSessions = table.Column<int>(type: "int", nullable: true),
                    ApprovalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstallmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCourseAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentCourseAgreements_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentCourseAgreements_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentCourseAgreements_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    CourseAgreementId = table.Column<int>(type: "int", nullable: false),
                    TreatmentPlanSessionId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    InsuranceCompanyId = table.Column<int>(type: "int", nullable: false),
                    ApprovalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubmissionReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_TreatmentCourseAgreements_CourseAgreementId",
                        column: x => x.CourseAgreementId,
                        principalTable: "TreatmentCourseAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_TreatmentPlanSessions_TreatmentPlanSessionId",
                        column: x => x.TreatmentPlanSessionId,
                        principalTable: "TreatmentPlanSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InsuranceClaimId",
                table: "Payments",
                column: "InsuranceClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CourseAgreementId_TreatmentPlanSessionId",
                table: "Invoices",
                columns: new[] { "CourseAgreementId", "TreatmentPlanSessionId" },
                unique: true,
                filter: "[CourseAgreementId] IS NOT NULL AND [TreatmentPlanSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TreatmentPlanSessionId",
                table: "Invoices",
                column: "TreatmentPlanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingBatches_CourseAgreementId",
                table: "BookingBatches",
                column: "CourseAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_CourseAgreementId",
                table: "InsuranceClaims",
                column: "CourseAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_IdempotencyKey",
                table: "InsuranceClaims",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_InsuranceCompanyId",
                table: "InsuranceClaims",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_InvoiceId",
                table: "InsuranceClaims",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PatientId",
                table: "InsuranceClaims",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_TreatmentPlanSessionId",
                table: "InsuranceClaims",
                column: "TreatmentPlanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCourseAgreements_InsuranceCompanyId",
                table: "TreatmentCourseAgreements",
                column: "InsuranceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCourseAgreements_PatientId",
                table: "TreatmentCourseAgreements",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCourseAgreements_TreatmentPlanId",
                table: "TreatmentCourseAgreements",
                column: "TreatmentPlanId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingBatches_TreatmentCourseAgreements_CourseAgreementId",
                table: "BookingBatches",
                column: "CourseAgreementId",
                principalTable: "TreatmentCourseAgreements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TreatmentCourseAgreements_CourseAgreementId",
                table: "Invoices",
                column: "CourseAgreementId",
                principalTable: "TreatmentCourseAgreements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TreatmentPlanSessions_TreatmentPlanSessionId",
                table: "Invoices",
                column: "TreatmentPlanSessionId",
                principalTable: "TreatmentPlanSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_InsuranceClaims_InsuranceClaimId",
                table: "Payments",
                column: "InsuranceClaimId",
                principalTable: "InsuranceClaims",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingBatches_TreatmentCourseAgreements_CourseAgreementId",
                table: "BookingBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TreatmentCourseAgreements_CourseAgreementId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TreatmentPlanSessions_TreatmentPlanSessionId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_InsuranceClaims_InsuranceClaimId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "InsuranceClaims");

            migrationBuilder.DropTable(
                name: "TreatmentCourseAgreements");

            migrationBuilder.DropIndex(
                name: "IX_Payments_InsuranceClaimId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CourseAgreementId_TreatmentPlanSessionId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TreatmentPlanSessionId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_BookingBatches_CourseAgreementId",
                table: "BookingBatches");

            migrationBuilder.DropColumn(
                name: "CollectionMethod",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InsuranceClaimId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayerType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CourseAgreementId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TreatmentPlanSessionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CourseAgreementId",
                table: "BookingBatches");

            migrationBuilder.AlterColumn<int>(
                name: "AppointmentId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
