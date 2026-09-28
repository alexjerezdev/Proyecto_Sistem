using System.Diagnostics;
using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace CafeAroma.Tests;

// PC-118 Rendimiento:
//  - Reportes (resumen del dia e historial de cierres) <= 3 s
//  - Cierre de caja <= 5 min
// Mide servidor + base de datos (no incluye red ni pantalla del frontend).
public class RendimientoCajaTests : IDisposable
{
    private const double LimiteReporteSegundos = 3;
    private const double LimiteCierreSegundos = 5 * 60;
    private const int Repeticiones = 5;

    private static readonly string ConnStr =
        Environment.GetEnvironmentVariable("CAFEAROMA_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=Proyecto_Aroma_Test;Username=postgres;Password=TU_CLAVE";

    private readonly ITestOutputHelper _salida;
    private readonly AppDbContext _context;
    private readonly CierreCajaController _controller;
    private readonly List<int> _cierresCreados = new();

    public RendimientoCajaTests(ITestOutputHelper salida)
    {
        _salida = salida;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnStr).Options;
        _context = new AppDbContext(options);
        _controller = new CierreCajaController(_context);
    }

    [Fact]
    public async Task ResumenDia_RespondeEn3SegundosOMenos()
    {
        var tiempos = new List<double>();
        for (int i = 0; i < Repeticiones; i++)
            tiempos.Add(await Medir(() => _controller.GetResumenDia()));

        _salida.WriteLine($"Resumen del dia: maximo {tiempos.Max():F3} s | promedio {tiempos.Average():F3} s");
        Assert.True(tiempos.Max() <= LimiteReporteSegundos,
            $"El resumen tardo {tiempos.Max():F2} s (limite {LimiteReporteSegundos} s).");
    }

    [Fact]
    public async Task HistorialCierres_RespondeEn3SegundosOMenos()
    {
        var tiempos = new List<double>();
        for (int i = 0; i < Repeticiones; i++)
            tiempos.Add(await Medir(() => _controller.GetHistorial(null, null, null, false)));

        _salida.WriteLine($"Historial de cierres: maximo {tiempos.Max():F3} s | promedio {tiempos.Average():F3} s");
        Assert.True(tiempos.Max() <= LimiteReporteSegundos,
            $"El historial tardo {tiempos.Max():F2} s (limite {LimiteReporteSegundos} s).");
    }

    [Fact]
    public async Task CierreDeCaja_TerminaEnMenosDe5Minutos()
    {
        var request = new CierreCajaRequest { UsuarioId = UsuarioId(), EfectivoContado = 100m };

        var reloj = Stopwatch.StartNew();
        var resp = await _controller.CerrarCaja(request);
        reloj.Stop();

        var creado = Assert.IsType<CreatedAtActionResult>(resp.Result);
        var cierre = Assert.IsType<CierreCaja>(creado.Value);
        _cierresCreados.Add(cierre.CierreCajaId);

        _salida.WriteLine($"Cierre de caja: {reloj.Elapsed.TotalSeconds:F3} s");
        Assert.True(reloj.Elapsed.TotalSeconds <= LimiteCierreSegundos,
            $"El cierre tardo {reloj.Elapsed.TotalSeconds:F2} s (limite {LimiteCierreSegundos} s).");
    }

    private static async Task<double> Medir(Func<Task> accion)
    {
        var reloj = Stopwatch.StartNew();
        await accion();
        reloj.Stop();
        return reloj.Elapsed.TotalSeconds;
    }

    private static int UsuarioId()
    {
        using var conn = new NpgsqlConnection(ConnStr);
        conn.Open();
        using var cmd = new NpgsqlCommand("SELECT usuario_id FROM usuario ORDER BY usuario_id LIMIT 1", conn);
        return Convert.ToInt32(cmd.ExecuteScalar()!);
    }

    // Borra el cierre y la bitacora que crearon las pruebas
    public void Dispose()
    {
        foreach (var id in _cierresCreados)
        {
            var bitacoras = _context.Bitacoras
                .Where(b => b.Accion == "CIERRE_CAJA" && b.EntidadId == id)
                .ToList();
            _context.Bitacoras.RemoveRange(bitacoras);

            var cierre = _context.CierresCaja.Find(id);
            if (cierre != null) _context.CierresCaja.Remove(cierre);
        }
        _context.SaveChanges();
        _context.Dispose();
    }
}