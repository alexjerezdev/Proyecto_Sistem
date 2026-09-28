using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CafeAroma.Tests;

public class InsumosControllerTests
{
    // Base de datos en memoria nueva y aislada para cada test.
    private AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // ---------- GetInsumos ----------

    [Fact]
    public async Task GetInsumos_DevuelveTodosLosInsumos()
    {
        var context = CrearContexto();
        context.Insumos.Add(new Insumo { Nombre = "Cafe", StockActual = 10, StockMinimo = 2 });
        context.Insumos.Add(new Insumo { Nombre = "Leche", StockActual = 5, StockMinimo = 1 });
        await context.SaveChangesAsync();

        var controller = new InsumosController(context);
        var resultado = await controller.GetInsumos();

        var lista = Assert.IsAssignableFrom<IEnumerable<Insumo>>(resultado.Value);
        Assert.Equal(2, lista.Count());
    }

    // ---------- GetInsumo(id) ----------

    [Fact]
    public async Task GetInsumo_IdExistente_DevuelveElInsumo()
    {
        var context = CrearContexto();
        var insumo = new Insumo { Nombre = "Azucar", StockActual = 20, StockMinimo = 5 };
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();

        var controller = new InsumosController(context);
        var resultado = await controller.GetInsumo(insumo.InsumoId);

        Assert.Equal("Azucar", resultado.Value?.Nombre);
    }

    [Fact]
    public async Task GetInsumo_IdInexistente_Devuelve404()
    {
        var context = CrearContexto();
        var controller = new InsumosController(context);

        var resultado = await controller.GetInsumo(999);

        Assert.IsType<NotFoundResult>(resultado.Result);
    }

    // ---------- CrearInsumo ----------

    [Fact]
    public async Task CrearInsumo_DatosValidos_LoAgregaYDevuelve201()
    {
        var context = CrearContexto();
        var controller = new InsumosController(context);
        var nuevo = new Insumo { Nombre = "Canela", StockActual = 3, StockMinimo = 1 };

        var resultado = await controller.CrearInsumo(nuevo);

        var creado = Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var insumoCreado = Assert.IsType<Insumo>(creado.Value);
        Assert.Equal("Canela", insumoCreado.Nombre);
        Assert.Single(context.Insumos);
    }

    // ---------- ActualizarStockMinimo ----------

    [Fact]
    public async Task ActualizarStockMinimo_IdExistente_ActualizaYDevuelve204()
    {
        var context = CrearContexto();
        var insumo = new Insumo { Nombre = "Cacao", StockActual = 8, StockMinimo = 2 };
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();

        var controller = new InsumosController(context);
        var resultado = await controller.ActualizarStockMinimo(insumo.InsumoId, 5);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(5, insumo.StockMinimo);
    }

    [Fact]
    public async Task ActualizarStockMinimo_IdInexistente_Devuelve404()
    {
        var context = CrearContexto();
        var controller = new InsumosController(context);

        var resultado = await controller.ActualizarStockMinimo(999, 5);

        Assert.IsType<NotFoundResult>(resultado);
    }

    // ---------- GetAlertas ----------

    [Fact]
    public async Task GetAlertas_SoloDevuelveNoAtendidas_OrdenadasPorFechaDesc()
    {
        var context = CrearContexto();
        context.AlertasStock.Add(new AlertaStock { InsumoId = 1, NivelDetectado = 1, Atendida = false, Fecha = DateTime.UtcNow.AddDays(-1) });
        context.AlertasStock.Add(new AlertaStock { InsumoId = 2, NivelDetectado = 1, Atendida = false, Fecha = DateTime.UtcNow });
        context.AlertasStock.Add(new AlertaStock { InsumoId = 3, NivelDetectado = 1, Atendida = true, Fecha = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = new InsumosController(context);
        var resultado = await controller.GetAlertas();

        var lista = Assert.IsAssignableFrom<IEnumerable<AlertaStock>>(resultado.Value).ToList();
        Assert.Equal(2, lista.Count);
        Assert.All(lista, a => Assert.False(a.Atendida));
        Assert.Equal(2, lista.First().InsumoId); // la mas reciente primero
    }

    // ---------- AtenderAlerta ----------

    [Fact]
    public async Task AtenderAlerta_IdExistente_LaMarcaAtendidaYDevuelve204()
    {
        var context = CrearContexto();
        var alerta = new AlertaStock { InsumoId = 1, NivelDetectado = 1, Atendida = false, Fecha = DateTime.UtcNow };
        context.AlertasStock.Add(alerta);
        await context.SaveChangesAsync();

        var controller = new InsumosController(context);
        var resultado = await controller.AtenderAlerta(alerta.AlertaId);

        Assert.IsType<NoContentResult>(resultado);
        Assert.True(alerta.Atendida);
    }

    [Fact]
    public async Task AtenderAlerta_IdInexistente_Devuelve404()
    {
        var context = CrearContexto();
        var controller = new InsumosController(context);

        var resultado = await controller.AtenderAlerta(999);

        Assert.IsType<NotFoundResult>(resultado);
    }

    // ---------- GetMovimientos ----------

    [Fact]
    public async Task GetMovimientos_FiltraPorInsumoYOrdenaPorFechaDesc()
    {
        var context = CrearContexto();
        context.MovimientosInventario.Add(new MovimientoInventario { InsumoId = 1, Tipo = "entrada", Cantidad = 10, Fecha = DateTime.UtcNow.AddDays(-2) });
        context.MovimientosInventario.Add(new MovimientoInventario { InsumoId = 1, Tipo = "salida", Cantidad = 2, Fecha = DateTime.UtcNow });
        context.MovimientosInventario.Add(new MovimientoInventario { InsumoId = 2, Tipo = "entrada", Cantidad = 5, Fecha = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = new InsumosController(context);
        var resultado = await controller.GetMovimientos(1);

        var lista = Assert.IsAssignableFrom<IEnumerable<MovimientoInventario>>(resultado.Value).ToList();
        Assert.Equal(2, lista.Count);
        Assert.All(lista, m => Assert.Equal(1, m.InsumoId));
        Assert.Equal("salida", lista.First().Tipo); // la mas reciente primero
    }

    [Fact]
    public async Task GetMovimientos_InsumoSinMovimientos_DevuelveListaVacia()
    {
        var context = CrearContexto();
        var controller = new InsumosController(context);

        var resultado = await controller.GetMovimientos(999);

        var lista = Assert.IsAssignableFrom<IEnumerable<MovimientoInventario>>(resultado.Value);
        Assert.Empty(lista);
    }
}