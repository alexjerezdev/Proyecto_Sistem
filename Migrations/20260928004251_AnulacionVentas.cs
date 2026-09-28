using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace luisfrontend.Migrations
{
    /// <inheritdoc />
    public partial class AnulacionVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Anulada",
                table: "Ventas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "Ventas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedioPago",
                table: "Ventas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "Ventas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioAnula",
                table: "Ventas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anulada",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MedioPago",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioAnula",
                table: "Ventas");
        }
    }
}
