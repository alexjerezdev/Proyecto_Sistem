using System.Diagnostics;
using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace CafeAroma.Tests;

// PC-118 Rendimiento: RNF "venta <= 5 s".
// Mide el tiempo del servidor + base de datos al registrar una venta
// (no incluye red ni pantalla del frontend).
public class RendimientoVentasTests : IDisposable
{
    private const int LimiteVentaSegundos = 5;
    private const int Repeticiones = 10;

    private static readonly string ConnStr =
        Environment.GetEnvironmentVariable("CAFEAROMA_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=Proyecto_Aroma_Test;Username=postgres;Password=TU_CLAVE";

    private readonly ITestOutputHelper _salida;
    private readonly AppDbContext _context;
    private readonly SincronizacionController _controller;
    private readonly List<Guid> _uuids = new();

    public RendimientoVentasTests(ITestOutputHelper salida)
    {
        _salida = salida;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnStr).Options;
        _context = new AppDbContext(options);
        _controller = new SincronizacionController(_context);
    }

    [Fact]
    public async Task Venta_SeRegistraEnMenosDe5Segundos()
    {
        var tiempos = new List<double>();

        for (int i = 0; i < Repeticiones; i++)
        {
            var venta = NuevaVenta();
            var reloj = Stopwatch.StartNew();
            var resp = await _controller.SincronizarVentas(new List<VentaOfflineRequest> { venta });
            reloj.Stop();

            Assert.Equal("sincronizada", resp.Value![0].Estado);
            tiempos.Add(reloj.Elapsed.TotalSeconds);
        }

        _salida.WriteLine($"Ventas: {Repeticiones} | maximo: {tiempos.Max():F3} s | promedio: {tiempos.Average():F3} s");
        Assert.True(tiempos.Max() <= LimiteVentaSegundos,
            $"Una venta tardo {tiempos.Max():F2} s (limite {LimiteVentaSegundos} s).");
    }

    private VentaOfflineRequest NuevaVenta()
    {
        var venta = new VentaOfflineRequest
        {
            OfflineId = Guid.NewGuid(),
            FechaHora = DateTimeOffset.UtcNow,
            UsuarioId = Escalar("SELECT usuario_id FROM usuario ORDER BY usuario_id LIMIT 1"),
            Detalles = new()
        };
        venta.Detalles.Add(new() { ProductoId = Escalar("SELECT producto_id FROM producto ORDER BY producto_id LIMIT 1"), Cantidad = 1, PrecioUnitario = 10m });
        _uuids.Add(venta.OfflineId);
        return venta;
    }

    private static int Escalar(string sql)
    {
        using var conn = new NpgsqlConnection(ConnStr);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        return Convert.ToInt32(cmd.ExecuteScalar()!);
    }

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