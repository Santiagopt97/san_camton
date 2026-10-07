using System.Text.Json;
using HabitacionesApi.Controllers;
using HabitacionesApi.Data;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace HabitacionesApi.Tests;

public class PublicoTests
{
    private static Habitacion Crear(HabitacionesDbContext db, string tipo, int capacidad, decimal precio, bool activo = true)
    {
        var h = new Habitacion
        {
            Id = Guid.NewGuid(), Numero = Guid.NewGuid().ToString("N")[..6], Tipo = tipo, Capacidad = capacidad,
            PrecioNoche = precio, Activo = activo, Piso = 3, Estado = "Ocupada", Descripcion = "descripción interna",
            CreadoEn = DateTime.UtcNow,
        };
        db.Habitaciones.Add(h);
        db.SaveChanges();
        return h;
    }

    private static IMemoryCache Cache() => new MemoryCache(new MemoryCacheOptions());

    private static async Task<List<HabitacionPublica>> Listar(HabitacionesDbContext db)
    {
        var r = await new PublicoController(db, Cache()).Listar(default);
        return Assert.IsAssignableFrom<IEnumerable<HabitacionPublica>>(Assert.IsType<OkObjectResult>(r).Value).ToList();
    }

    // Review Focus 2
    [Fact]
    public async Task Solo_aparecen_las_habitaciones_activas()
    {
        var db = Datos.NuevoDb();
        Crear(db, "Doble", 2, 180000);
        Crear(db, "Suite", 3, 420000, activo: false);
        var lista = await Listar(db);
        Assert.Equal("Doble", Assert.Single(lista).Tipo);
    }

    // Review Focus 2
    [Fact]
    public async Task Un_tipo_con_todas_sus_habitaciones_de_baja_desaparece()
    {
        var db = Datos.NuevoDb();
        Crear(db, "Suite", 3, 420000, activo: false);
        Crear(db, "Suite", 3, 450000, activo: false);
        Assert.Empty(await Listar(db));
    }

    [Fact]
    public async Task Agrupa_por_tipo_con_la_capacidad_maxima_y_el_precio_minimo()
    {
        var db = Datos.NuevoDb();
        Crear(db, "Doble", 2, 200000);
        Crear(db, "Doble", 3, 180000);
        var t = Assert.Single(await Listar(db));
        Assert.Equal(3, t.Capacidad);
        Assert.Equal(180000m, t.PrecioDesde);
    }

    [Fact]
    public async Task Ordena_por_precio_desde_y_luego_por_tipo()
    {
        var db = Datos.NuevoDb();
        Crear(db, "Suite", 3, 420000);
        Crear(db, "Sencilla", 1, 120000);
        Crear(db, "Familiar", 4, 120000);
        Crear(db, "Doble", 2, 180000);
        Assert.Equal(new[] { "Familiar", "Sencilla", "Doble", "Suite" }, (await Listar(db)).Select(x => x.Tipo));
    }

    [Fact]
    public async Task Las_imagenes_empiezan_por_la_habitacion_mas_barata_en_su_orden_y_son_maximo_6()
    {
        var db = Datos.NuevoDb();
        var cara = Crear(db, "Doble", 2, 200000);
        var barata = Crear(db, "Doble", 2, 150000);
        var deLaCara = Enumerable.Range(0, 4).Select(i => Datos.Imagen(db, cara.Id, i).Url).ToList();
        var deLaBarata = Enumerable.Range(0, 4).Select(i => Datos.Imagen(db, barata.Id, i).Url).ToList();

        var t = Assert.Single(await Listar(db));

        Assert.Equal(6, t.Imagenes.Count);
        Assert.Equal(deLaBarata.Concat(deLaCara.Take(2)).ToList(), t.Imagenes.Select(i => i.Url).ToList());
    }

    // Review Focus 3
    [Fact]
    public async Task Un_tipo_sin_imagenes_devuelve_una_lista_vacia()
    {
        var db = Datos.NuevoDb();
        Crear(db, "Sencilla", 1, 120000);
        Assert.Empty(Assert.Single(await Listar(db)).Imagenes);
    }

    // Review Focus 3
    [Fact]
    public async Task Sin_habitaciones_devuelve_una_lista_vacia()
    {
        Assert.Empty(await Listar(Datos.NuevoDb()));
    }

    // Review Focus 1
    [Fact]
    public async Task No_expone_datos_internos()
    {
        var db = Datos.NuevoDb();
        var h = Crear(db, "Doble", 2, 180000);
        Datos.Imagen(db, h.Id, 0);

        var json = JsonSerializer.Serialize(await Listar(db), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var elemento = JsonDocument.Parse(json).RootElement[0];
        Assert.Equal(new[] { "capacidad", "imagenes", "precioDesde", "tipo" }, elemento.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray());
        Assert.Equal(new[] { "url" }, elemento.GetProperty("imagenes")[0].EnumerateObject().Select(p => p.Name).ToArray());
        Assert.DoesNotContain(h.Numero, json);
        Assert.DoesNotContain("Ocupada", json);
        Assert.DoesNotContain("descripción interna", json);
        // La URL pública de cada imagen lleva el id de la habitación en su ruta (así se guardan en el bucket): no se expone como campo, pero va dentro de la URL
    }

    // Review: la caché de 60 s debe existir en el servidor, no solo en las cabeceras (un cliente puede ignorarlas)
    [Fact]
    public async Task La_lista_se_calcula_una_vez_por_minuto_y_se_sirve_de_la_cache()
    {
        var db = Datos.NuevoDb();
        var cache = Cache();
        Crear(db, "Doble", 2, 180000);
        var controlador = new PublicoController(db, cache);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<HabitacionPublica>>(Assert.IsType<OkObjectResult>(await controlador.Listar(default)).Value));

        Crear(db, "Suite", 3, 420000); // cambia la base, pero la lista guardada sigue vigente
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<HabitacionPublica>>(Assert.IsType<OkObjectResult>(await controlador.Listar(default)).Value));

        // con una caché nueva (o pasado el minuto) se ve el cambio
        var fresco = new PublicoController(db, Cache());
        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<HabitacionPublica>>(Assert.IsType<OkObjectResult>(await fresco.Listar(default)).Value).Count());
    }
}
