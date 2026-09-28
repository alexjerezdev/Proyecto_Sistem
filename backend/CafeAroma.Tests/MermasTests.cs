using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CafeAroma.Tests;

// HU-11 Mermas (RF18): al registrar una merma se descuenta del stock y queda el motivo guardado.
[Collection("BaseDeDatos")]
public class MermasTests : IDisposable
{
    private static readonly string ConnStr =
        Environment.GetEnvironmentVariable("CAFEAROMA_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=Proyecto_Aroma_Test;Username=postgres;Password=TU_CLAVE";

    private readonly AppDbContext _context;
    private readonly MermasController _controller;
    private readonly List<int> _insumos = new();

    public MermasTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnStr).Options;
        _context = new AppDbContext(options);
        _controller = new MermasController(_context);
    }

    [Fact]
    public async Task Merma_DescuentaDelStock()
    {
        var id = CrearInsumo(10m);

        var resp = await _controller.Registrar(new MermaDto { InsumoId = id, Cantidad = 3m, Motivo = "Vencido" });

        Assert.IsType<OkObjectResult>(resp);
        Assert.Equal(7m, Stock(id));
    }

    [Fact]
    public async Task Merma_GuardaElMotivo()
    {
        var id = CrearInsumo(10m);

        await _controller.Registrar(new MermaDto { InsumoId = id, Cantidad = 2m, Motivo = "  Dañado en transporte " });

        var motivo = Escalar<string>("SELECT motivo FROM merma WHERE insumo_id = @i", ("i", id));
        Assert.Equal("Dañado en transporte", motivo);
    }

    [Fact]
    public async Task Merma_CantidadMayorAlStock_Devuelve400_YNoCambiaNada()
    {
        var id = CrearInsumo(5m);

        var resp = await _controller.Registrar(new MermaDto { InsumoId = id, Cantidad = 6m, Motivo = "Vencido" });

        Assert.IsType<BadRequestObjectResult>(resp);
        Assert.Equal(5m, Stock(id));
        Assert.Equal(0L, Escalar<long>("SELECT COUNT(*) FROM merma WHERE insumo_id = @i", ("i", id)));
    }

    [Fact]
    public async Task Merma_SinMotivo_Devuelve400()
    {
        var id = CrearInsumo(5m);

        var resp = await _controller.Registrar(new MermaDto { InsumoId = id, Cantidad = 1m, Motivo = "   " });

        Assert.IsType<BadRequestObjectResult>(resp);
        Assert.Equal(5m, Stock(id));
    }

    [Fact]
    public async Task Merma_CantidadCero_Devuelve400()
    {
        var id = CrearInsumo(5m);

        var resp = await _controller.Registrar(new MermaDto { InsumoId = id, Cantidad = 0m, Motivo = "Dañado" });

        Assert.IsType<BadRequestObjectResult>(resp);
    }

    [Fact]
    public async Task Merma_InsumoInexistente_Devuelve404()
    {
        var resp = await _controller.Registrar(new MermaDto { InsumoId = int.MaxValue, Cantidad = 1m, Motivo = "Dañado" });

        Assert.IsType<NotFoundObjectResult>(resp);
    }

    // ---------- Ayudas ----------

    private int CrearInsumo(decimal stock)
    {
        var id = Escalar<int>(
            "INSERT INTO insumo (nombre, stock_actual, stock_minimo) VALUES (@n, @s, 0) RETURNING insumo_id",
            ("n", "Insumo prueba merma"), ("s", stock));
        _insumos.Add(id);
        return id;
    }

    private static decimal Stock(int id) =>
        Escalar<decimal>("SELECT stock_actual FROM insumo WHERE insumo_id = @i", ("i", id));

    private static T Escalar<T>(string sql, params (string nombre, object valor)[] parametros)
    {
        using var conn = new NpgsqlConnection(ConnStr);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (nombre, valor) in parametros)
            cmd.Parameters.AddWithValue(nombre, valor);
        return (T)Convert.ChangeType(cmd.ExecuteScalar()!, typeof(T));
    }

    // Borra las mermas e insumos que crearon las pruebas
    public void Dispose()
    {
        using (var conn = new NpgsqlConnection(ConnStr))
        {
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                DELETE FROM merma WHERE insumo_id = ANY(@i);
                DELETE FROM insumo WHERE insumo_id = ANY(@i);", conn);
            cmd.Parameters.AddWithValue("i", _insumos.ToArray());
            cmd.ExecuteNonQuery();
        }
        _context.Dispose();
    }
}