using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CafeAroma.Tests;

// Pruebas de cierre de caja (POST api/CierreCaja) y resumen del dia.
// Se prueban contra una base Postgres real (copia de prueba) porque:
//  - "diferencia" es una columna GENERATED de Postgres (efectivo_contado - total_ventas),
//    InMemory no la calcula.
//  - el total de ventas se obtiene con SQL propio de Postgres (fecha_hora::date = CURRENT_DATE).
//
// Convencion de la diferencia: efectivo contado - total del sistema.
//   0  = caja cuadrada | negativo = faltante | positivo = sobrante
//
// Base de prueba: variable de entorno CAFEAROMA_TEST_DB (asi la clave no queda en el codigo).
// Requiere al menos un usuario en la base de prueba.
[Collection("BaseDeDatos")] // evita correr en paralelo con otras pruebas que tocan la misma base
public class CierreCajaTests : IDisposable
{
    private static readonly string ConnStr =
        Environment.GetEnvironmentVariable("CAFEAROMA_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=Proyecto_Aroma_Test;Username=postgres;Password=TU_CLAVE";

    private readonly AppDbContext _context;
    private readonly CierreCajaController _controller;
    private readonly List<int> _cierres = new();
    private readonly List<int> _ventas = new();

    public CierreCajaTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnStr)
            .Options;
        _context = new AppDbContext(options);
        _controller = new CierreCajaController(_context);
    }

    // ---------- Cierre de caja y calculo de diferencia ----------

    [Fact]
    public async Task Cierre_EfectivoIgualAlTotal_DiferenciaCero()
    {
        var total = TotalVentasHoy();

        var cierre = await Cerrar(total);

        Assert.Equal(0m, cierre.Diferencia);
    }

    [Fact]
    public async Task Cierre_EfectivoMenorAlTotal_DiferenciaNegativaFaltante()
    {
        var total = TotalVentasHoy();

        var cierre = await Cerrar(total - 15m);

        Assert.Equal(-15m, cierre.Diferencia);
    }

    [Fact]
    public async Task Cierre_EfectivoMayorAlTotal_DiferenciaPositivaSobrante()
    {
        var total = TotalVentasHoy();

        var cierre = await Cerrar(total + 20m);

        Assert.Equal(20m, cierre.Diferencia);
    }

    [Fact]
    public async Task Cierre_LaDiferenciaQuedaGuardadaEnLaBase()
    {
        var total = TotalVentasHoy();

        var cierre = await Cerrar(total + 7.5m);

        var enBd = Escalar<decimal>(
            "SELECT diferencia FROM cierre_caja WHERE cierre_caja_id = @id", ("id", cierre.CierreCajaId));
        Assert.Equal(7.5m, enBd);
    }

    // ---------- Total de ventas del cierre ----------

    [Fact]
    public async Task Cierre_TotalVentasEsLaSumaDeLasVentasDeHoy()
    {
        var antes = TotalVentasHoy();
        InsertarVenta(30m);
        InsertarVenta(20.5m);

        var cierre = await Cerrar(0m);

        Assert.Equal(antes + 50.5m, cierre.TotalVentas);
    }

    [Fact]
    public async Task Cierre_VentasDeAyerNoCuentan()
    {
        var antes = TotalVentasHoy();
        InsertarVenta(999.99m, "CURRENT_TIMESTAMP - INTERVAL '1 day'");

        var cierre = await Cerrar(0m);

        Assert.Equal(antes, cierre.TotalVentas);
    }

    [Fact]
    public async Task Cierre_GuardaFechaDeHoyYUsuarioYEfectivo()
    {
        var cierre = await Cerrar(100m);

        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), cierre.Fecha);
        Assert.Equal(UsuarioId(), cierre.UsuarioId);
        Assert.Equal(100m, cierre.EfectivoContado);
    }

    // ---------- Resumen del dia, detalle e historial ----------

    [Fact]
    public async Task ResumenDia_DevuelveElTotalDeVentasDeHoy()
    {
        var antes = TotalVentasHoy();
        InsertarVenta(12.34m);

        var resp = await _controller.GetResumenDia();

        var total = (decimal)resp.Value!.GetType().GetProperty("totalVentasHoy")!.GetValue(resp.Value)!;
        Assert.Equal(antes + 12.34m, total);
    }

    [Fact]
    public async Task GetCierre_Existente_DevuelveElCierre()
    {
        var cierre = await Cerrar(50m);

        var resp = await _controller.GetCierre(cierre.CierreCajaId);

        Assert.Equal(cierre.CierreCajaId, resp.Value!.CierreCajaId);
    }

    [Fact]
    public async Task GetCierre_Inexistente_Devuelve404()
    {
        var resp = await _controller.GetCierre(int.MaxValue);

        Assert.IsType<NotFoundResult>(resp.Result);
    }

    [Fact]
    public async Task Historial_MuestraPrimeroElCierreMasReciente()
    {
        var primero = await Cerrar(10m);
        await Task.Delay(50);
        var segundo = await Cerrar(20m);

        var resp = await _controller.GetHistorial(null, null, null);

        var lista = resp.Value!.ToList();
        var posPrimero = lista.FindIndex(c => c.CierreCajaId == primero.CierreCajaId);
        var posSegundo = lista.FindIndex(c => c.CierreCajaId == segundo.CierreCajaId);
        Assert.True(posSegundo >= 0 && posPrimero >= 0);
        Assert.True(posSegundo < posPrimero, "El cierre mas reciente deberia aparecer antes.");
    }

    // ---------- Bitacora (HU-26) ----------

    [Fact]
    public async Task Cierre_DejaConstanciaEnLaBitacora()
    {
        var total = TotalVentasHoy();

        var cierre = await Cerrar(total + 5m);

        var filas = Escalar<long>(
            @"SELECT COUNT(*) FROM bitacora
              WHERE accion = 'CIERRE_CAJA' AND entidad_afectada = 'cierre_caja' AND entidad_id = @id",
            ("id", cierre.CierreCajaId));
        Assert.Equal(1, filas);

        var detalle = Escalar<string>(
            "SELECT detalle FROM bitacora WHERE accion = 'CIERRE_CAJA' AND entidad_id = @id",
            ("id", cierre.CierreCajaId));
        Assert.Contains("diferencia", detalle);
    }

    // ---------- Filtros del historial (HU-22) ----------

    [Fact]
    public async Task Historial_SoloConDiferencia_ExcluyeLosCierresCuadrados()
    {
        var total = TotalVentasHoy();
        var cuadrado = await Cerrar(total);
        var conDiferencia = await Cerrar(total + 5m);

        var resp = await _controller.GetHistorial(null, null, null, soloConDiferencia: true);

        var lista = resp.Value!.ToList();
        Assert.Contains(lista, c => c.CierreCajaId == conDiferencia.CierreCajaId);
        Assert.DoesNotContain(lista, c => c.CierreCajaId == cuadrado.CierreCajaId);
        Assert.All(lista, c => Assert.NotEqual(0m, c.Diferencia));
    }

    [Fact]
    public async Task Historial_FiltraPorUsuario()
    {
        var cierre = await Cerrar(10m);

        var delUsuario = await _controller.GetHistorial(null, null, UsuarioId());
        var deOtro = await _controller.GetHistorial(null, null, int.MaxValue);

        Assert.Contains(delUsuario.Value!, c => c.CierreCajaId == cierre.CierreCajaId);
        Assert.Empty(deOtro.Value!);
    }

    [Fact]
    public async Task Historial_FiltraPorRangoDeFechas()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var cierre = await Cerrar(10m);

        var incluyeHoy = await _controller.GetHistorial(hoy, hoy, null);
        var desdeManana = await _controller.GetHistorial(hoy.AddDays(1), null, null);
        var hastaAyer = await _controller.GetHistorial(null, hoy.AddDays(-1), null);

        Assert.Contains(incluyeHoy.Value!, c => c.CierreCajaId == cierre.CierreCajaId);
        Assert.DoesNotContain(desdeManana.Value!, c => c.CierreCajaId == cierre.CierreCajaId);
        Assert.DoesNotContain(hastaAyer.Value!, c => c.CierreCajaId == cierre.CierreCajaId);
    }

    // ---------- Ayudas ----------

    private async Task<CierreCaja> Cerrar(decimal efectivo)
    {
        var resp = await _controller.CerrarCaja(new CierreCajaRequest
        {
            UsuarioId = UsuarioId(),
            EfectivoContado = efectivo
        });
        var creado = Assert.IsType<CreatedAtActionResult>(resp.Result);
        var cierre = Assert.IsType<CierreCaja>(creado.Value);
        _cierres.Add(cierre.CierreCajaId);
        return cierre;
    }

    // fechaSql solo recibe constantes escritas en las pruebas, nunca datos externos.
    private void InsertarVenta(decimal total, string fechaSql = "CURRENT_TIMESTAMP")
    {
        var id = Escalar<int>(
            $"INSERT INTO venta (fecha_hora, total, usuario_id) VALUES ({fechaSql}, @t, @u) RETURNING venta_id",
            ("t", total), ("u", UsuarioId()));
        _ventas.Add(id);
    }

    private static decimal TotalVentasHoy() =>
        Escalar<decimal>("SELECT COALESCE(SUM(total), 0) FROM venta WHERE fecha_hora::date = CURRENT_DATE");

    private static int UsuarioId() =>
        Escalar<int>("SELECT usuario_id FROM usuario ORDER BY usuario_id LIMIT 1");

    private static T Escalar<T>(string sql, params (string nombre, object valor)[] parametros)
    {
        using var conn = new NpgsqlConnection(ConnStr);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nombre, valor) in parametros)
            cmd.Parameters.AddWithValue(nombre, valor);
        return (T)Convert.ChangeType(cmd.ExecuteScalar()!, typeof(T));
    }

    // Borra los cierres y ventas que crearon las pruebas
    public void Dispose()
    {
        using (var conn = new NpgsqlConnection(ConnStr))
        {
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                DELETE FROM bitacora WHERE accion = 'CIERRE_CAJA' AND entidad_id = ANY(@c);
                DELETE FROM cierre_caja WHERE cierre_caja_id = ANY(@c);
                DELETE FROM venta WHERE venta_id = ANY(@v);", conn);
            cmd.Parameters.AddWithValue("c", _cierres.ToArray());
            cmd.Parameters.AddWithValue("v", _ventas.ToArray());
            cmd.ExecuteNonQuery();
        }
        _context.Dispose();
    }
}