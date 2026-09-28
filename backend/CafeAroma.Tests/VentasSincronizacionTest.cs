using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CafeAroma.Tests;

// Pruebas de integracion de ventas (POST api/Sincronizacion/ventas).
// El stock lo descuenta un trigger de PostgreSQL, por eso se prueba contra una
// base real (copia de prueba), no con mocks ni InMemory.
//
// Base de prueba: Proyecto_Aroma_Test (copia de Proyecto_Aroma).
// Se puede cambiar con la variable de entorno CAFEAROMA_TEST_DB.
public class VentasSincronizacionTests : IDisposable
{
    private static readonly string ConnStr =
        Environment.GetEnvironmentVariable("CAFEAROMA_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=Proyecto_Aroma_Test;Username=postgres;Password=TU_CLAVE";

    private readonly AppDbContext _context;
    private readonly SincronizacionController _controller;
    private readonly List<Guid> _uuids = new();

    public VentasSincronizacionTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnStr)
            .Options;
        _context = new AppDbContext(options);
        _controller = new SincronizacionController(_context);
    }

    // ---------- Registro de venta ----------

    [Fact]
    public async Task RegistroVenta_Valida_QuedaSincronizadaYGuardada()
    {
        var venta = NuevaVenta((ProductoId(), 1, 10m));

        var r = await Sincronizar(venta);

        Assert.Equal("sincronizada", r.Estado);
        Assert.NotNull(r.VentaId);
        var filas = Escalar<long>("SELECT COUNT(*) FROM venta WHERE offline_uuid = @u", ("u", venta.OfflineId));
        Assert.Equal(1, filas);
    }

    [Fact]
    public async Task RegistroVenta_MismoOfflineIdDosVeces_NoSeDuplica()
    {
        var venta = NuevaVenta((ProductoId(), 1, 10m));

        var primera = await Sincronizar(venta);
        var segunda = await Sincronizar(venta);

        Assert.Equal("sincronizada", primera.Estado);
        Assert.Equal("duplicada", segunda.Estado);
        var filas = Escalar<long>("SELECT COUNT(*) FROM venta WHERE offline_uuid = @u", ("u", venta.OfflineId));
        Assert.Equal(1, filas);
    }

    [Fact]
    public async Task RegistroVenta_SinOfflineId_DaError()
    {
        var venta = NuevaVenta((ProductoId(), 1, 10m));
        venta.OfflineId = Guid.Empty;

        var r = await Sincronizar(venta);

        Assert.Equal("error", r.Estado);
        Assert.Contains("OfflineId", r.Mensaje);
    }

    [Fact]
    public async Task RegistroVenta_SinDetalles_DaError()
    {
        var venta = NuevaVenta(); // sin detalles

        var r = await Sincronizar(venta);

        Assert.Equal("error", r.Estado);
        Assert.Contains("no tiene detalles", r.Mensaje);
    }

    [Fact]
    public async Task RegistroVenta_CantidadCero_DaError()
    {
        var venta = NuevaVenta((ProductoId(), 0, 10m));

        var r = await Sincronizar(venta);

        Assert.Equal("error", r.Estado);
        Assert.Contains("invalidos", r.Mensaje);
    }

    // ---------- Calculo del total ----------

    [Fact]
    public async Task Total_SumaCantidadPorPrecio()
    {
        var prod = ProductoId();
        // 2 x 10.00 + 3 x 5.50 = 36.50
        var venta = NuevaVenta((prod, 2, 10m), (prod, 3, 5.5m));

        var r = await Sincronizar(venta);

        Assert.Equal("sincronizada", r.Estado);
        var total = Escalar<decimal>("SELECT total FROM venta WHERE offline_uuid = @u", ("u", venta.OfflineId));
        Assert.Equal(36.5m, total);
    }

    [Fact]
    public async Task Total_LoCalculaElServidor_NoElDelDispositivo()
    {
        // El request no trae total: el servidor lo calcula desde los detalles.
        var venta = NuevaVenta((ProductoId(), 4, 2.25m));

        await Sincronizar(venta);

        var total = Escalar<decimal>("SELECT total FROM venta WHERE offline_uuid = @u", ("u", venta.OfflineId));
        Assert.Equal(9m, total);
    }

    // ---------- Descuento de stock (trigger de la base de datos) ----------
    // Estas dos pruebas asumen que el primer producto tiene insumos en su receta.
    // Si fallan por eso, hay que fijar un producto que si los tenga.

    [Fact]
    public async Task Stock_AlVender_BajaElStockDeInsumos()
    {
        var antes = Escalar<decimal>("SELECT COALESCE(SUM(stock_actual), 0) FROM insumo");
        var venta = NuevaVenta((ProductoId(), 1, 10m));

        var r = await Sincronizar(venta);

        Assert.Equal("sincronizada", r.Estado);
        var despues = Escalar<decimal>("SELECT COALESCE(SUM(stock_actual), 0) FROM insumo");
        Assert.True(despues < antes, $"El stock no bajo (antes={antes}, despues={despues}).");
    }

    [Fact]
    public async Task Stock_SiNoAlcanza_DaErrorYNoGuardaLaVenta()
    {
        var venta = NuevaVenta((ProductoId(), 1_000_000, 10m));

        var r = await Sincronizar(venta);

        Assert.Equal("error", r.Estado);
        Assert.Contains("No alcanza el stock", r.Mensaje);
        var filas = Escalar<long>("SELECT COUNT(*) FROM venta WHERE offline_uuid = @u", ("u", venta.OfflineId));
        Assert.Equal(0, filas);
    }

    // ---------- Ayudas ----------

    private VentaOfflineRequest NuevaVenta(params (int producto, int cantidad, decimal precio)[] items)
    {
        var venta = new VentaOfflineRequest
        {
            OfflineId = Guid.NewGuid(),
            FechaHora = DateTimeOffset.UtcNow,
            UsuarioId = UsuarioId(),
            Detalles = new()
        };
        foreach (var i in items)
            venta.Detalles.Add(new() { ProductoId = i.producto, Cantidad = i.cantidad, PrecioUnitario = i.precio });
        _uuids.Add(venta.OfflineId);
        return venta;
    }

    private async Task<ResultadoSincronizacion> Sincronizar(VentaOfflineRequest venta)
    {
        var resp = await _controller.SincronizarVentas(new List<VentaOfflineRequest> { venta });
        return resp.Value![0];
    }

    private static int UsuarioId() => Escalar<int>("SELECT usuario_id FROM usuario ORDER BY usuario_id LIMIT 1");
    private static int ProductoId() => Escalar<int>("SELECT producto_id FROM producto ORDER BY producto_id LIMIT 1");

    private static T Escalar<T>(string sql, params (string nombre, object valor)[] parametros)
    {
        using var conn = new NpgsqlConnection(ConnStr);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nombre, valor) in parametros)
            cmd.Parameters.AddWithValue(nombre, valor);
        return (T)Convert.ChangeType(cmd.ExecuteScalar()!, typeof(T));
    }

    // Borra las ventas que crearon las pruebas (el stock descontado queda en la base de prueba)
    public void Dispose()
    {
        if (_uuids.Count > 0)
        {
            using var conn = new NpgsqlConnection(ConnStr);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                DELETE FROM detalle_venta WHERE venta_id IN
                    (SELECT venta_id FROM venta WHERE offline_uuid = ANY(@u));
                DELETE FROM venta WHERE offline_uuid = ANY(@u);", conn);
            cmd.Parameters.AddWithValue("u", _uuids.ToArray());
            cmd.ExecuteNonQuery();
        }
        _context.Dispose();
    }
}