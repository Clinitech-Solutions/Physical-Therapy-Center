using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Never silently truncate legacy staff identifiers while adding the conflict index.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Appointments] WHERE LEN([SeniorId]) > 450) THROW 51000, 'Review legacy SeniorId values longer than 450 characters before this migration.', 1;");
            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_RoomId",
                table: "Appointments");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "Assessments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Assessments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SeniorId",
                table: "Appointments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualCheckIn",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualCheckOut",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingBatchId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingSequence",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InsuranceFunded",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BookingBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PricingMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PatientAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InsuranceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WaiverReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_BookingBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingBatches_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Backfill funding only where an existing invoice is evidence of insurer liability.
            // Clinical completion and actual attendance are intentionally not inferred from severity or planned times.
            // Compile the backfill only after ADD COLUMN, including in a single-batch idempotent script.
            migrationBuilder.Sql("EXEC(N'UPDATE a SET [InsuranceFunded] = 1 FROM [Appointments] a WHERE EXISTS (SELECT 1 FROM [Invoices] i WHERE i.[AppointmentId] = a.[Id] AND i.[InsuranceAmount] > 0 AND i.[IsDeleted] = 0);');");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_AppointmentId",
                table: "Assessments",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BookingBatchId_BookingSequence",
                table: "Appointments",
                columns: new[] { "BookingBatchId", "BookingSequence" },
                unique: true,
                filter: "[BookingBatchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_StartTime",
                table: "Appointments",
                columns: new[] { "PatientId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RoomId_StartTime",
                table: "Appointments",
                columns: new[] { "RoomId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_SeniorId_StartTime",
                table: "Appointments",
                columns: new[] { "SeniorId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingBatches_IdempotencyKey",
                table: "BookingBatches",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingBatches_PatientId",
                table: "BookingBatches",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_BookingBatches_BookingBatchId",
                table: "Appointments",
                column: "BookingBatchId",
                principalTable: "BookingBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assessments_Appointments_AppointmentId",
                table: "Assessments",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_BookingBatches_BookingBatchId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assessments_Appointments_AppointmentId",
                table: "Assessments");

            migrationBuilder.DropTable(
                name: "BookingBatches");

            migrationBuilder.DropIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_AppointmentId",
                table: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_BookingBatchId_BookingSequence",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_RoomId_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_SeniorId_StartTime",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "ActualCheckIn",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ActualCheckOut",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BookingBatchId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BookingSequence",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "InsuranceFunded",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "SeniorId",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RoomId",
                table: "Appointments",
                column: "RoomId");
        }
    }
}
