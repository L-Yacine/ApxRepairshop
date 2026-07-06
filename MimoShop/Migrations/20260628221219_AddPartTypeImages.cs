using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MimoShop.Migrations
{
    /// <inheritdoc />
    public partial class AddPartTypeImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "PartTypes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "PartTypes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "PartTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ImageUrl", "ThumbnailUrl" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "PartTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ImageUrl", "ThumbnailUrl" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "PartTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ImageUrl", "ThumbnailUrl" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "PartTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ImageUrl", "ThumbnailUrl" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "PartTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ImageUrl", "ThumbnailUrl" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "PartTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ImageUrl", "ThumbnailUrl" },
                values: new object[] { "", "" });

            migrationBuilder.UpdateData(
                table: "PartTypes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ImageUrl", "ThumbnailUrl" },
                values: new object[] { "", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "PartTypes");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "PartTypes");
        }
    }
}
