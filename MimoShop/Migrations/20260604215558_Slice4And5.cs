using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MimoShop.Migrations
{
    /// <inheritdoc />
    public partial class Slice4And5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "RepairTickets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "InventoryParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Brand = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PartType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Variant = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitSalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsStocked = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryParts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepairPartUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairTicketId = table.Column<int>(type: "int", nullable: false),
                    InventoryPartId = table.Column<int>(type: "int", nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PartType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Variant = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitSalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsOnDemand = table.Column<bool>(type: "bit", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUsername = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairPartUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairPartUsages_InventoryParts_InventoryPartId",
                        column: x => x.InventoryPartId,
                        principalTable: "InventoryParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RepairPartUsages_RepairTickets_RepairTicketId",
                        column: x => x.RepairTicketId,
                        principalTable: "RepairTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryStockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryPartId = table.Column<int>(type: "int", nullable: false),
                    RepairTicketId = table.Column<int>(type: "int", nullable: true),
                    RepairPartUsageId = table.Column<int>(type: "int", nullable: true),
                    QuantityChange = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUsername = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryStockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryStockMovements_InventoryParts_InventoryPartId",
                        column: x => x.InventoryPartId,
                        principalTable: "InventoryParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryStockMovements_RepairPartUsages_RepairPartUsageId",
                        column: x => x.RepairPartUsageId,
                        principalTable: "RepairPartUsages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryStockMovements_RepairTickets_RepairTicketId",
                        column: x => x.RepairTicketId,
                        principalTable: "RepairTickets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryParts_Brand_Model_PartType_Variant",
                table: "InventoryParts",
                columns: new[] { "Brand", "Model", "PartType", "Variant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStockMovements_InventoryPartId",
                table: "InventoryStockMovements",
                column: "InventoryPartId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStockMovements_RepairPartUsageId",
                table: "InventoryStockMovements",
                column: "RepairPartUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStockMovements_RepairTicketId",
                table: "InventoryStockMovements",
                column: "RepairTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPartUsages_InventoryPartId",
                table: "RepairPartUsages",
                column: "InventoryPartId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairPartUsages_RepairTicketId",
                table: "RepairPartUsages",
                column: "RepairTicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryStockMovements");

            migrationBuilder.DropTable(
                name: "RepairPartUsages");

            migrationBuilder.DropTable(
                name: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "RepairTickets");
        }
    }
}
