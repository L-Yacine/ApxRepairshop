using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MimoShop.Migrations
{
    /// <inheritdoc />
    public partial class InventoryLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryParts_Brand_Model_PartType_Variant",
                table: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "PartType",
                table: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "Variant",
                table: "InventoryParts");

            migrationBuilder.RenameColumn(
                name: "Variant",
                table: "RepairPartUsages",
                newName: "PhoneModelName");

            migrationBuilder.RenameColumn(
                name: "PartType",
                table: "RepairPartUsages",
                newName: "PartVariantName");

            migrationBuilder.RenameColumn(
                name: "Model",
                table: "RepairPartUsages",
                newName: "PartTypeName");

            migrationBuilder.RenameColumn(
                name: "Brand",
                table: "RepairPartUsages",
                newName: "BrandName");

            migrationBuilder.AddColumn<int>(
                name: "BrandId",
                table: "InventoryParts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PartTypeId",
                table: "InventoryParts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PartVariantId",
                table: "InventoryParts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhoneModelId",
                table: "InventoryParts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartVariants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneModels_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Brands",
                columns: new[] { "Id", "DisplayNameAr", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "سامسونج", true, "Samsung", 10 },
                    { 2, "أبل", true, "Apple", 20 },
                    { 3, "شاومي", true, "Xiaomi", 30 },
                    { 4, "هواوي", true, "Huawei", 40 },
                    { 5, "أوبو", true, "Oppo", 50 },
                    { 6, "ريلمي", true, "Realme", 60 },
                    { 7, "إنفينيكس", true, "Infinix", 70 },
                    { 8, "تكنو", true, "Tecno", 80 },
                    { 9, "نوكيا", true, "Nokia", 90 },
                    { 10, "ون بلس", true, "OnePlus", 100 }
                });

            migrationBuilder.InsertData(
                table: "PartTypes",
                columns: new[] { "Id", "DisplayNameAr", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "شاشة", true, "Screen", 10 },
                    { 2, "بطارية", true, "Battery", 20 },
                    { 3, "غطاء خلفي", true, "Back cover", 30 },
                    { 4, "منفذ شحن", true, "Charging port", 40 },
                    { 5, "كاميرا", true, "Camera", 50 },
                    { 6, "سماعة", true, "Speaker", 60 },
                    { 7, "مايكروفون", true, "Microphone", 70 }
                });

            migrationBuilder.InsertData(
                table: "PartVariants",
                columns: new[] { "Id", "DisplayNameAr", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "أصلي", true, "Original OEM", 10 },
                    { 2, "متوافق", true, "Compatible", 20 },
                    { 3, "مجدد", true, "Refurbished", 30 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryParts_BrandId_PhoneModelId_PartTypeId_PartVariantId",
                table: "InventoryParts",
                columns: new[] { "BrandId", "PhoneModelId", "PartTypeId", "PartVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryParts_PartTypeId",
                table: "InventoryParts",
                column: "PartTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryParts_PartVariantId",
                table: "InventoryParts",
                column: "PartVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryParts_PhoneModelId",
                table: "InventoryParts",
                column: "PhoneModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartTypes_Name",
                table: "PartTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartVariants_Name",
                table: "PartVariants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneModels_BrandId_Name",
                table: "PhoneModels",
                columns: new[] { "BrandId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryParts_Brands_BrandId",
                table: "InventoryParts",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryParts_PartTypes_PartTypeId",
                table: "InventoryParts",
                column: "PartTypeId",
                principalTable: "PartTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryParts_PartVariants_PartVariantId",
                table: "InventoryParts",
                column: "PartVariantId",
                principalTable: "PartVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryParts_PhoneModels_PhoneModelId",
                table: "InventoryParts",
                column: "PhoneModelId",
                principalTable: "PhoneModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryParts_Brands_BrandId",
                table: "InventoryParts");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryParts_PartTypes_PartTypeId",
                table: "InventoryParts");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryParts_PartVariants_PartVariantId",
                table: "InventoryParts");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryParts_PhoneModels_PhoneModelId",
                table: "InventoryParts");

            migrationBuilder.DropTable(
                name: "PartTypes");

            migrationBuilder.DropTable(
                name: "PartVariants");

            migrationBuilder.DropTable(
                name: "PhoneModels");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_InventoryParts_BrandId_PhoneModelId_PartTypeId_PartVariantId",
                table: "InventoryParts");

            migrationBuilder.DropIndex(
                name: "IX_InventoryParts_PartTypeId",
                table: "InventoryParts");

            migrationBuilder.DropIndex(
                name: "IX_InventoryParts_PartVariantId",
                table: "InventoryParts");

            migrationBuilder.DropIndex(
                name: "IX_InventoryParts_PhoneModelId",
                table: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "PartTypeId",
                table: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "PartVariantId",
                table: "InventoryParts");

            migrationBuilder.DropColumn(
                name: "PhoneModelId",
                table: "InventoryParts");

            migrationBuilder.RenameColumn(
                name: "PhoneModelName",
                table: "RepairPartUsages",
                newName: "Variant");

            migrationBuilder.RenameColumn(
                name: "PartVariantName",
                table: "RepairPartUsages",
                newName: "PartType");

            migrationBuilder.RenameColumn(
                name: "PartTypeName",
                table: "RepairPartUsages",
                newName: "Model");

            migrationBuilder.RenameColumn(
                name: "BrandName",
                table: "RepairPartUsages",
                newName: "Brand");

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "InventoryParts",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "InventoryParts",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PartType",
                table: "InventoryParts",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Variant",
                table: "InventoryParts",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryParts_Brand_Model_PartType_Variant",
                table: "InventoryParts",
                columns: new[] { "Brand", "Model", "PartType", "Variant" },
                unique: true);
        }
    }
}
