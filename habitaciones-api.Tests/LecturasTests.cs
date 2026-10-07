using HabitacionesApi.Controllers;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabitacionesApi.Tests;

public class LecturasTests
{
    private static List<Habitacion> Items(IActionResult r)
    {
        var valor = Assert.IsType<OkObjectResult>(r).Value!;
        return ((IEnumerable<Habitacion>)valor.GetType().GetProperty("items")!.GetValue(valor)!).ToList();
    }

    [Fact]
    public async Task El_listado_incluye_las_imagenes_de_cada_habitacion_en_orden()
    {
        var db = Datos.NuevoDb(); var h1 = Datos.Habitacion(db); var h2 = Datos.Habitacion(db);
        var segunda = Datos.Imagen(db, h1.Id, 1); var primera = Datos.Imagen(db, h1.Id, 0);

        var items = Items(await new HabitacionesController(db).Listar(null, null, null, false));

        var de1 = items.Single(h => h.Id == h1.Id);
        Assert.Equal(new[] { primera.Id, segunda.Id }, de1.Imagenes.Select(i => i.Id));
        Assert.Equal(primera.Url, de1.Imagenes[0].Url);
        Assert.Empty(items.Single(h => h.Id == h2.Id).Imagenes);
    }

    [Fact]
    public async Task El_detalle_incluye_las_imagenes()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db);
        var img = Datos.Imagen(db, h.Id, 0);

        var r = await new HabitacionesController(db).Obtener(h.Id);

        var habitacion = Assert.IsType<Habitacion>(Assert.IsType<OkObjectResult>(r).Value);
        Assert.Equal(img.Id, Assert.Single(habitacion.Imagenes).Id);
    }

    [Fact]
    public async Task El_detalle_de_una_habitacion_sin_imagenes_trae_una_lista_vacia()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db);
        var habitacion = Assert.IsType<Habitacion>(Assert.IsType<OkObjectResult>(await new HabitacionesController(db).Obtener(h.Id)).Value);
        Assert.Empty(habitacion.Imagenes);
    }
}
