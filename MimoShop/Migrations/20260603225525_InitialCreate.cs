using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MimoShop.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WhatsApp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Telegram = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepairTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    DeviceBrand = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ProblemDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AssignedWorkerUsername = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AssignedWorkerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairTickets_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RepairStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairTicketId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUsername = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairStatusHistories_RepairTickets_RepairTicketId",
                        column: x => x.RepairTicketId,
                        principalTable: "RepairTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "StaffMembers",
                columns: new[] { "Id", "DisplayName", "PasswordHash", "Role", "Salt", "Username" },
                values: new object[,]
                {
                    { 1, "صاحب المحل", "D1FF07603ECECF1A10F3A4D9E9A9250A549EE45DC6851A47CE3CDA183A8E4BFE", "Owner", "mimoshop-v1-owner", "owner" },
                    { 2, "العامل 1", "550C8B3DBB4C0CDE5D49120D06729FE6591FE0C7D7392F045AE75DF3C22DD5D3", "Worker", "mimoshop-v1-worker1", "worker1" },
                    { 3, "العامل 2", "1F90ADDB5ADB8FC4E4F3149FC1B4E9421A3587A5D1807F71E0083ADB93AEBFAF", "Worker", "mimoshop-v1-worker2", "worker2" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairStatusHistories_RepairTicketId",
                table: "RepairStatusHistories",
                column: "RepairTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_CustomerId",
                table: "RepairTickets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_JobCode",
                table: "RepairTickets",
                column: "JobCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Username",
                table: "StaffMembers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepairStatusHistories");

            migrationBuilder.DropTable(
                name: "StaffMembers");

            migrationBuilder.DropTable(
                name: "RepairTickets");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
