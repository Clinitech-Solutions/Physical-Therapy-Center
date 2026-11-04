using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClinicianAbsenceRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                table: "DoctorAbsences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoryJson",
                table: "DoctorAbsences",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "OperationKey",
                table: "DoctorAbsences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "DoctorAbsences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DoctorAbsences",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "Withdrawn",
                table: "DoctorAbsences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AbsenceId",
                table: "AppointmentScheduleChanges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "AppointmentScheduleChanges",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Reschedule");

            migrationBuilder.AddColumn<string>(
                name: "ClinicianName",
                table: "AppointmentScheduleChanges",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentMethod",
                table: "AppointmentScheduleChanges",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentReason",
                table: "AppointmentScheduleChanges",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentRecordedAt",
                table: "AppointmentScheduleChanges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactOutcome",
                table: "AppointmentScheduleChanges",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GenderException",
                table: "AppointmentScheduleChanges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreviousClinicianId",
                table: "AppointmentScheduleChanges",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousClinicianName",
                table: "AppointmentScheduleChanges",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAbsences_OperationKey",
                table: "DoctorAbsences",
                column: "OperationKey",
                unique: true,
                filter: "[OperationKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAbsences_StartsAt_EndsAt",
                table: "DoctorAbsences",
                columns: new[] { "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentScheduleChanges_AbsenceId",
                table: "AppointmentScheduleChanges",
                column: "AbsenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentScheduleChanges_AppointmentId_AbsenceId_ChangedAt",
                table: "AppointmentScheduleChanges",
                columns: new[] { "AppointmentId", "AbsenceId", "ChangedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentScheduleChanges_DoctorAbsences_AbsenceId",
                table: "AppointmentScheduleChanges",
                column: "AbsenceId",
                principalTable: "DoctorAbsences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentScheduleChanges_DoctorAbsences_AbsenceId",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropIndex(
                name: "IX_DoctorAbsences_OperationKey",
                table: "DoctorAbsences");

            migrationBuilder.DropIndex(
                name: "IX_DoctorAbsences_StartsAt_EndsAt",
                table: "DoctorAbsences");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentScheduleChanges_AbsenceId",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentScheduleChanges_AppointmentId_AbsenceId_ChangedAt",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "DoctorAbsences");

            migrationBuilder.DropColumn(
                name: "HistoryJson",
                table: "DoctorAbsences");

            migrationBuilder.DropColumn(
                name: "OperationKey",
                table: "DoctorAbsences");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "DoctorAbsences");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DoctorAbsences");

            migrationBuilder.DropColumn(
                name: "Withdrawn",
                table: "DoctorAbsences");

            migrationBuilder.DropColumn(
                name: "AbsenceId",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "ClinicianName",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "ConsentMethod",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "ConsentReason",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "ConsentRecordedAt",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "ContactOutcome",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "GenderException",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "PreviousClinicianId",
                table: "AppointmentScheduleChanges");

            migrationBuilder.DropColumn(
                name: "PreviousClinicianName",
                table: "AppointmentScheduleChanges");
        }
    }
}
