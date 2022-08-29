using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChemicalsBase.Migrations
{
    public partial class AddingBiocideProductGroupToChemical : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BiocideProductGroup",
                table: "ChemicalVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BiocideProductType",
                table: "ChemicalVersions",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "BiocideProductGroup",
                table: "Chemicals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BiocideProductType",
                table: "Chemicals",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BiocideProductGroup",
                table: "ChemicalVersions");

            migrationBuilder.DropColumn(
                name: "BiocideProductType",
                table: "ChemicalVersions");

            migrationBuilder.DropColumn(
                name: "BiocideProductGroup",
                table: "Chemicals");

            migrationBuilder.DropColumn(
                name: "BiocideProductType",
                table: "Chemicals");
        }
    }
}
