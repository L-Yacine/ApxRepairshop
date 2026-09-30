using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MimoShop.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicStorefront : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wilayas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wilayas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Communes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WilayaId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Communes_Wilayas_WilayaId",
                        column: x => x.WilayaId,
                        principalTable: "Wilayas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CustomerWhatsApp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    WilayaId = table.Column<int>(type: "int", nullable: false),
                    CommuneId = table.Column<int>(type: "int", nullable: false),
                    WilayaName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CommuneName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CommuneNameFr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShipmentId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "New"),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopOrders_Communes_CommuneId",
                        column: x => x.CommuneId,
                        principalTable: "Communes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopOrders_Wilayas_WilayaId",
                        column: x => x.WilayaId,
                        principalTable: "Wilayas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopOrderId = table.Column<int>(type: "int", nullable: false),
                    InventoryPartId = table.Column<int>(type: "int", nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PartTypeName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PartDisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopOrderLines_InventoryParts_InventoryPartId",
                        column: x => x.InventoryPartId,
                        principalTable: "InventoryParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShopOrderLines_ShopOrders_ShopOrderId",
                        column: x => x.ShopOrderId,
                        principalTable: "ShopOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Wilayas",
                columns: new[] { "Id", "Code", "IsActive", "NameAr", "NameFr" },
                values: new object[,]
                {
                    { 1, "01", true, "أدرار", "Adrar" },
                    { 2, "02", true, "الشلف", "Chlef" },
                    { 3, "03", true, "الأغواط", "Laghouat" },
                    { 4, "04", true, "أم البواقي", "Oum El Bouaghi" },
                    { 5, "05", true, "باتنة", "Batna" },
                    { 6, "06", true, "بجاية", "Béjaïa" },
                    { 7, "07", true, "بسكرة", "Biskra" },
                    { 8, "08", true, "بشار", "Béchar" },
                    { 9, "09", true, "البليدة", "Blida" },
                    { 10, "10", true, "البويرة", "Bouira" },
                    { 11, "11", true, "تمنراست", "Tamanrasset" },
                    { 12, "12", true, "تبسة", "Tébessa" },
                    { 13, "13", true, "تلمسان", "Tlemcen" },
                    { 14, "14", true, "تيارت", "Tiaret" },
                    { 15, "15", true, "تيزي وزو", "Tizi Ouzou" },
                    { 16, "16", true, "الجزائر", "Alger" },
                    { 17, "17", true, "الجلفة", "Djelfa" },
                    { 18, "18", true, "جيجل", "Jijel" },
                    { 19, "19", true, "سطيف", "Sétif" },
                    { 20, "20", true, "سعيدة", "Saïda" },
                    { 21, "21", true, "سكيكدا", "Skikda" },
                    { 22, "22", true, "سيدي بلعباس", "Sidi Bel Abbès" },
                    { 23, "23", true, "عنابة", "Annaba" },
                    { 24, "24", true, "قالمة", "Guelma" },
                    { 25, "25", true, "قسنطينة", "Constantine" },
                    { 26, "26", true, "المدية", "Médéa" },
                    { 27, "27", true, "مستغانم", "Mostaganem" },
                    { 28, "28", true, "المسيلة", "M'Sila" },
                    { 29, "29", true, "معسكر", "Mascara" },
                    { 30, "30", true, "ورقلة", "Ouargla" },
                    { 31, "31", true, "وهران", "Oran" },
                    { 32, "32", true, "البيض", "El Bayadh" },
                    { 33, "33", true, "إليزي", "Illizi" },
                    { 34, "34", true, "برج بوعريريج", "Bordj Bou Arréridj" },
                    { 35, "35", true, "بومرداس", "Boumerdès" },
                    { 36, "36", true, "الطريف", "El Tarf" },
                    { 37, "37", true, "تندوف", "Tindouf" },
                    { 38, "38", true, "تيسمسيلت", "Tissemsilt" },
                    { 39, "39", true, "الوادي", "El Oued" },
                    { 40, "40", true, "خنشلة", "Khenchela" },
                    { 41, "41", true, "سوق أهراس", "Souk Ahras" },
                    { 42, "42", true, "تيبازة", "Tipaza" },
                    { 43, "43", true, "ميلة", "Mila" },
                    { 44, "44", true, "عين الدفلى", "Aïn Defla" },
                    { 45, "45", true, "النعامة", "Naâma" },
                    { 46, "46", true, "عين تموشنت", "Aïn Témouchent" },
                    { 47, "47", true, "غرداية", "Ghardaïa" },
                    { 48, "48", true, "غليزان", "Relizane" },
                    { 49, "49", true, "تيميمون", "Timimoun" },
                    { 50, "50", true, "برج باجي مختار", "Bordj Badji Mokhtar" },
                    { 51, "51", true, "أولاد جلال", "Ouled Djellal" },
                    { 52, "52", true, "بني عباس", "Béni Abbès" },
                    { 53, "53", true, "عين صالح", "In Salah" },
                    { 54, "54", true, "عين قزام", "In Guezzam" },
                    { 55, "55", true, "تقرت", "Touggourt" },
                    { 56, "56", true, "جانت", "Djanet" },
                    { 57, "57", true, "المغير", "El M'Ghair" },
                    { 58, "58", true, "المنية", "El Meniaa" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Communes_WilayaId_NameAr",
                table: "Communes",
                columns: new[] { "WilayaId", "NameAr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrderLines_InventoryPartId",
                table: "ShopOrderLines",
                column: "InventoryPartId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrderLines_ShopOrderId",
                table: "ShopOrderLines",
                column: "ShopOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_CommuneId",
                table: "ShopOrders",
                column: "CommuneId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_OrderCode",
                table: "ShopOrders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_WilayaId",
                table: "ShopOrders",
                column: "WilayaId");

            migrationBuilder.CreateIndex(
                name: "IX_Wilayas_Code",
                table: "Wilayas",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wilayas_NameAr",
                table: "Wilayas",
                column: "NameAr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wilayas_NameFr",
                table: "Wilayas",
                column: "NameFr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopOrderLines");

            migrationBuilder.DropTable(
                name: "ShopOrders");

            migrationBuilder.DropTable(
                name: "Communes");

            migrationBuilder.DropTable(
                name: "Wilayas");
        }
    }
}
