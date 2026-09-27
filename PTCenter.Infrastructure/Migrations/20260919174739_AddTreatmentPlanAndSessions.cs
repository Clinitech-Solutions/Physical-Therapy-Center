using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentPlanAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlanSessions_TreatmentPlanId",
                table: "TreatmentPlanSessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckIn",
                table: "TreatmentPlanSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOut",
                table: "TreatmentPlanSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "TreatmentPlanSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionDate",
                table: "TreatmentPlanSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "e4b2f91d-9e12-4f4a-b3db-1b58fca15e66",
                columns: new[] { "FullName", "NormalizedUserName", "UserName" },
                values: new object[] { "SuperAdmin", "SUPERADMIN", "SuperAdmin" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanSessions_TreatmentPlanId_SessionNumber",
                table: "TreatmentPlanSessions",
                columns: new[] { "TreatmentPlanId", "SessionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlanSessions_TreatmentPlanId_SessionNumber",
                table: "TreatmentPlanSessions");

            migrationBuilder.DropColumn(
                name: "CheckIn",
                table: "TreatmentPlanSessions");

            migrationBuilder.DropColumn(
                name: "CheckOut",
                table: "TreatmentPlanSessions");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "TreatmentPlanSessions");

            migrationBuilder.DropColumn(
                name: "SessionDate",
                table: "TreatmentPlanSessions");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "e4b2f91d-9e12-4f4a-b3db-1b58fca15e66",
                columns: new[] { "FullName", "NormalizedUserName", "UserName" },
                values: new object[] { "admin", "ADMIN", "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanSessions_TreatmentPlanId",
                table: "TreatmentPlanSessions",
                column: "TreatmentPlanId");
        }
    }
}
