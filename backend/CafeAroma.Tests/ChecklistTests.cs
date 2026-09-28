using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CafeAroma.Tests;

// HU-12 Diferencias (RF19): checklist completado -> StockTeorico vs StockContado por insumo.
// Diferencia = contado - teorico (negativo = faltante, positivo = sobrante).
[Collection("BaseDeDatos")]
public class ChecklistTests : IDisposable
{
    private static readonly string ConnStr =
        Environment.GetEnvironmentVariable("CAFEAROMA_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=Proyecto_Aroma_Test;Username=postgres;Password=TU_CLAVE";

    private readonly AppDbContext _context;
    private readonly ChecklistsController _controller;
    private readonly List<int> _insumos = new();
    private readonly List<int> _checklists = new();

    public ChecklistTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnStr).Options;
        _context = new AppDbContext(options);
        _controller = new ChecklistsController(_context);
    }

    [Fact]
    public async Task Diferencia_ContadoMenor_EsFaltante()
    {
        var id = CrearInsumo(10m);
        var d = await Completar((id, 8m));
        Assert.Equal(10m, d.StockTeorico);
        Assert.Equal(8m, d.StockContado);
        Assert.Equal(-2m, d.Diferencia);
        Assert.Equal("Faltante", d.Estado);
    }

    [Fact]
    public async Task Diferencia_ContadoMayor_EsSobrante()
    {
        var id = CrearInsumo(10m);
        var d = await Completar((id, 12m));
        Assert.Equal(2m, d.Diferencia);
        Assert.Equal("Sobrante", d.Estado);
    }

    [Fact]
    public async Task Diferencia_ContadoIgual_Cuadra()
    {
        var id = CrearInsumo(10m);
        var d = await Completar((id, 10m));
        Assert.Equal(0m, d.Diferencia);
        Assert.Equal("Cuadra", d.Estado);
    }

    [Fact]
    public async Task Checklist_MuestraUnaDiferenciaPorInsumo()
    {
        var a = CrearInsumo(10m);
        var b = CrearInsumo(4m);
        var r = await CompletarChecklist((a, 9m), (b, 6m));
        Assert.Equal(2, r.Diferencias.Count);
        Assert.Equal(-1m, r.Diferencias.Single(x => x.InsumoId == a).Diferencia);
        Assert.Equal(2m, r.Diferencias.Single(x => x.InsumoId == b).Diferencia);
    }

    [Fact]
    public async Task StockTeorico_QuedaFijoAunqueElStockCambieDespues()
    {
        var id = CrearInsumo(10m);
        var r = await CompletarChecklist((id, 8m));
        Ejecutar("UPDATE insumo SET stock_actual = 1 WHERE insumo_id = @i", ("i", id));

        var resp = await _controller.GetDiferencias(r.ChecklistId);

        var ok = Assert.IsType<OkObjectResult>(resp);
        var d = Assert.Single(Assert.IsType<ChecklistResultado>(ok.Value).Diferencias);
        Assert.Equal(10m, d.StockTeorico);
        Assert.Equal(-2m, d.Diferencia);
    }

    [Fact]
    public async Task ChecklistSinInsumos_Devuelve400()
    {
        var resp = await _controller.Completar(new ChecklistRequest { UsuarioId = 1 });
        Assert.IsType<BadRequestObjectResult>(resp);
    }

    [Fact]
    public async Task StockContadoNegativo_Devuelve400()
    {
        var id = CrearInsumo(5m);
        var resp = await _controller.Completar(Req((id, -1m)));
        Assert.IsType<BadRequestObjectResult>(resp);
    }

    [Fact]
    public async Task InsumoInexistente_Devuelve404()
    {
        var resp = await _controller.Completar(Req((int.MaxValue, 1m)));
        Assert.IsType<NotFoundObjectResult>(resp);
    }

    [Fact]
    public async Task GetDiferencias_ChecklistInexistente_Devuelve404()
    {
        var resp = await _controller.GetDiferencias(int.MaxValue);
        Assert.IsType<NotFoundResult>(resp);
    }

    // ---------- Ayudas ----------

    private static ChecklistRequest Req(params (int insumo, decimal contado)[] lineas) => new()
    {
        UsuarioId = UsuarioId(),
        Lineas = lineas.Select(l => new LineaConteoDto { InsumoId = l.insumo, StockContado = l.contado }).ToList()
    };

    private async Task<ChecklistResultado> CompletarChecklist(params (int insumo, decimal contado)[] lineas)
    {
        var resp = await _controller.Completar(Req(lineas));
        var creado = Assert.IsType<CreatedAtActionResult>(resp);
        var r = Assert.IsType<ChecklistResultado>(creado.Value);
        _checklists.Add(r.ChecklistId);
        return r;
    }

    private async Task<DiferenciaDto> Completar((int insumo, decimal contado) linea) =>
        Assert.Single((await CompletarChecklist(linea)).Diferencias);

    private int CrearInsumo(decimal stock)
    {
        var id = Escalar<int>(
            "INSERT INTO insumo (nombre, stock_actual, stock_minimo) VALUES (@n, @s, 0) RETURNING insumo_id",
            ("n", "Insumo prueba checklist"), ("s", stock));
        _insumos.Add(id);
        return id;
    }

    private static int UsuarioId() =>
        Escalar<int>("SELECT usuario_id FROM usuario ORDER BY usuario_id LIMIT 1");

    private static void Ejecutar(string sql, params (string nombre, object valor)[] parametros)
    {
        using var conn = new NpgsqlConnection(ConnStr);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nombre, valor) in parametros) cmd.Parameters.AddWithValue(nombre, valor);
        cmd.ExecuteNonQuery();
    }

    private static T Escalar<T>(string sql, params (string nombre, object valor)[] parametros)
    {
        using var conn = new NpgsqlConnection(ConnStr);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nombre, valor) in parametros) cmd.Parameters.AddWithValue(nombre, valor);
        return (T)Convert.ChangeType(cmd.ExecuteScalar()!, typeof(T));
    }

    // Borra los checklists e insumos que crearon las pruebas
    public void Dispose()
    {
        using (var conn = new NpgsqlConnection(ConnStr))
        {
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                DELETE FROM checklist WHERE checklist_id = ANY(@c);
                DELETE FROM insumo WHERE insumo_id = ANY(@i);", conn);
            cmd.Parameters.AddWithValue("c", _checklists.ToArray());
            cmd.Parameters.AddWithValue("i", _insumos.ToArray());
            cmd.ExecuteNonQuery();
        }
        _context.Dispose();
    }
}