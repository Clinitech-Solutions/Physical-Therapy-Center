using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PTCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoles2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0191d311-2918-7f76-bd8a-0bdf740adffa", "0191d311-2918-7f76-bd8a-0be100f11386", "Senior", "SENIOR" },
                    { "0191d311-2918-7f76-bd8a-0bdf740adffb", "0191d311-2918-7f76-bd8a-0be100f11387", "Receptionist", "RECEPTIONIST" },
                    { "0191d311-2918-7f76-bd8a-0bdf740adffc", "0191d311-2918-7f76-bd8a-0be100f11388", "Ceo", "CEO" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0191d311-2918-7f76-bd8a-0bdf740adffa");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0191d311-2918-7f76-bd8a-0bdf740adffb");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0191d311-2918-7f76-bd8a-0bdf740adffc");
        }
    }
}
