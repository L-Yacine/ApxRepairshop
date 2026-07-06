using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MimoShop.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StaffMembers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "StaffMembers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DisplayName", "IsActive", "Role" },
                values: new object[] { "مدير النظام", true, "SuperAdmin" });

            migrationBuilder.UpdateData(
                table: "StaffMembers",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "StaffMembers",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsActive",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StaffMembers");

            migrationBuilder.UpdateData(
                table: "StaffMembers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DisplayName", "Role" },
                values: new object[] { "صاحب المحل", "Owner" });
        }
    }
}
