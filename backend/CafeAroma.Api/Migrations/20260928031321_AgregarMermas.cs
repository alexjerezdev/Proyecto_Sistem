using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CafeAroma.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMermas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alerta_stock",
                columns: table => new
                {
                    alerta_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nivel_detectado = table.Column<decimal>(type: "numeric", nullable: false),
                    atendida = table.Column<bool>(type: "boolean", nullable: false),
                    insumo_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerta_stock", x => x.alerta_id);
                });

            migrationBuilder.CreateTable(
                name: "bitacora",
                columns: table => new
                {
                    bitacora_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    accion = table.Column<string>(type: "text", nullable: false),
                    entidad_afectada = table.Column<string>(type: "text", nullable: false),
                    entidad_id = table.Column<int>(type: "integer", nullable: true),
                    detalle = table.Column<string>(type: "text", nullable: true),
                    fecha_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bitacora", x => x.bitacora_id);
                });

            migrationBuilder.CreateTable(
                name: "cierre_caja",
                columns: table => new
                {
                    cierre_caja_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    total_ventas = table.Column<decimal>(type: "numeric", nullable: false),
                    efectivo_contado = table.Column<decimal>(type: "numeric", nullable: false),
                    diferencia = table.Column<decimal>(type: "numeric", nullable: false),
                    fecha_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cierre_caja", x => x.cierre_caja_id);
                });

            migrationBuilder.CreateTable(
                name: "historico_precio",
                columns: table => new
                {
                    historico_precio_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<int>(type: "integer", nullable: false),
                    precio_anterior = table.Column<decimal>(type: "numeric", nullable: false),
                    precio_nuevo = table.Column<decimal>(type: "numeric", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_precio", x => x.historico_precio_id);
                });

            migrationBuilder.CreateTable(
                name: "insumo",
                columns: table => new
                {
                    insumo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    stock_actual = table.Column<decimal>(type: "numeric", nullable: false),
                    stock_minimo = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insumo", x => x.insumo_id);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_inventario",
                columns: table => new
                {
                    movimiento_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric", nullable: false),
                    insumo_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_inventario", x => x.movimiento_id);
                });

            migrationBuilder.CreateTable(
                name: "producto_insumo",
                columns: table => new
                {
                    producto_id = table.Column<int>(type: "integer", nullable: false),
                    insumo_id = table.Column<int>(type: "integer", nullable: false),
                    cantidad_necesaria = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producto_insumo", x => new { x.producto_id, x.insumo_id });
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    rol = table.Column<string>(type: "text", nullable: false),
                    contrasena_hash = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.usuario_id);
                });

            migrationBuilder.CreateTable(
                name: "merma",
                columns: table => new
                {
                    merma_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    insumo_id = table.Column<int>(type: "integer", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric", nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merma", x => x.merma_id);
                    table.ForeignKey(
                        name: "FK_merma_insumo_insumo_id",
                        column: x => x.insumo_id,
                        principalTable: "insumo",
                        principalColumn: "insumo_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_merma_insumo_id",
                table: "merma",
                column: "insumo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerta_stock");

            migrationBuilder.DropTable(
                name: "bitacora");

            migrationBuilder.DropTable(
                name: "cierre_caja");

            migrationBuilder.DropTable(
                name: "historico_precio");

            migrationBuilder.DropTable(
                name: "merma");

            migrationBuilder.DropTable(
                name: "movimiento_inventario");

            migrationBuilder.DropTable(
                name: "producto_insumo");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "insumo");
        }
    }
}
