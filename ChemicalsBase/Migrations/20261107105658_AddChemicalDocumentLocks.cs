using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChemicalsBase.Migrations
{
    /// <inheritdoc />
    public partial class AddChemicalDocumentLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChemicalDocumentLocks",
                columns: table => new
                {
                    ChemicalId = table.Column<int>(type: "int", nullable: false),
                    Counter = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalDocumentLocks", x => x.ChemicalId);
                    table.ForeignKey(
                        name: "FK_ChemicalDocumentLocks_Chemicals_ChemicalId",
                        column: x => x.ChemicalId,
                        principalTable: "Chemicals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChemicalDocumentLocks");
        }
    }
}
