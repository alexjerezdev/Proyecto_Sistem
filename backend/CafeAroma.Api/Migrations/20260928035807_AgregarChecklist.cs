using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CafeAroma.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checklist",
                columns: table => new
                {
                    checklist_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist", x => x.checklist_id);
                });

            migrationBuilder.CreateTable(
                name: "checklist_detalle",
                columns: table => new
                {
                    detalle_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    checklist_id = table.Column<int>(type: "integer", nullable: false),
                    insumo_id = table.Column<int>(type: "integer", nullable: false),
                    stock_teorico = table.Column<decimal>(type: "numeric", nullable: false),
                    stock_contado = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_detalle", x => x.detalle_id);
                    table.ForeignKey(
                        name: "FK_checklist_detalle_checklist_checklist_id",
                        column: x => x.checklist_id,
                        principalTable: "checklist",
                        principalColumn: "checklist_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_checklist_detalle_insumo_insumo_id",
                        column: x => x.insumo_id,
                        principalTable: "insumo",
                        principalColumn: "insumo_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checklist_detalle_checklist_id",
                table: "checklist_detalle",
                column: "checklist_id");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_detalle_insumo_id",
                table: "checklist_detalle",
                column: "insumo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checklist_detalle");

            migrationBuilder.DropTable(
                name: "checklist");
        }
    }
}
