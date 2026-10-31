using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AppointmentScheduleAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentScheduleChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    TreatmentPlanId = table.Column<int>(type: "int", nullable: true),
                    OperationKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousRoomId = table.Column<int>(type: "int", nullable: true),
                    PreviousRoomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NewStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewRoomId = table.Column<int>(type: "int", nullable: false),
                    NewRoomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClinicianId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentScheduleChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentScheduleChanges_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentScheduleChanges_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentScheduleChanges_AppointmentId",
                table: "AppointmentScheduleChanges",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentScheduleChanges_OperationKey",
                table: "AppointmentScheduleChanges",
                column: "OperationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentScheduleChanges_PatientId_TreatmentPlanId_ChangedAt",
                table: "AppointmentScheduleChanges",
                columns: new[] { "PatientId", "TreatmentPlanId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentScheduleChanges");
        }
    }
}
