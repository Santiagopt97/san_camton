using HabitacionesApi.Controllers;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HabitacionesApi.Tests;

public class BorrarReordenarTests
{
    private static HabitacionImagenesController Crear(Data.HabitacionesDbContext db, StorageFalso st) =>
        new(db, st, NullLogger<HabitacionImagenesController>.Instance);

    [Fact]
    public async Task Borrar_quita_la_fila_borra_el_objeto_y_compacta_el_orden()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var a = Datos.Imagen(db, h.Id, 0); var b = Datos.Imagen(db, h.Id, 1); var c = Datos.Imagen(db, h.Id, 2);

        var r = await Crear(db, st).Borrar(h.Id, b.Id, default);

        Assert.IsType<NoContentResult>(r);
        Assert.Equal(new[] { b.Ruta }, st.Borrados);
        var restantes = await db.HabitacionImagenes.OrderBy(i => i.Orden).ToListAsync();
        Assert.Equal(new[] { a.Id, c.Id }, restantes.Select(i => i.Id));
        Assert.Equal(new[] { 0, 1 }, restantes.Select(i => i.Orden));
    }

    // Review Focus 3
    [Fact]
    public async Task Borrar_funciona_aunque_Storage_falle_o_el_objeto_ya_no_exista()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso { FallaAlBorrar = true };
        var a = Datos.Imagen(db, h.Id, 0);

        var r = await Crear(db, st).Borrar(h.Id, a.Id, default);

        Assert.IsType<NoContentResult>(r);
        Assert.Empty(db.HabitacionImagenes);
    }

    // Review: el borrado del objeto es de mejor esfuerzo; ni un timeout ni un error inesperado deben llegar al usuario
    [Fact]
    public async Task Borrar_funciona_aunque_Storage_lance_un_timeout()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db);
        var st = new StorageFalso { ExcepcionAlBorrar = new TaskCanceledException("timeout") };
        var a = Datos.Imagen(db, h.Id, 0); var b = Datos.Imagen(db, h.Id, 1);

        var r = await Crear(db, st).Borrar(h.Id, a.Id, default);

        Assert.IsType<NoContentResult>(r);
        var resto = Assert.Single(db.HabitacionImagenes);
        Assert.Equal(b.Id, resto.Id);
        Assert.Equal(0, resto.Orden); // el orden se compactó aunque Storage falló
    }

    // Review Focus 5
    [Fact]
    public async Task Borrar_una_imagen_de_otra_habitacion_es_404_y_no_toca_nada()
    {
        var db = Datos.NuevoDb(); var h1 = Datos.Habitacion(db); var h2 = Datos.Habitacion(db); var st = new StorageFalso();
        var ajena = Datos.Imagen(db, h2.Id, 0);

        var r = await Crear(db, st).Borrar(h1.Id, ajena.Id, default);

        Assert.IsType<NotFoundResult>(r);
        Assert.Single(db.HabitacionImagenes);
        Assert.Empty(st.Borrados);
    }

    [Fact]
    public async Task Borrar_en_habitacion_inexistente_es_404_y_en_una_dada_de_baja_es_409()
    {
        var db = Datos.NuevoDb(); var baja = Datos.Habitacion(db, activo: false); var st = new StorageFalso();
        var img = Datos.Imagen(db, baja.Id, 0);
        var c = Crear(db, st);

        Assert.IsType<NotFoundResult>(await c.Borrar(Guid.NewGuid(), img.Id, default));
        Assert.IsType<ConflictObjectResult>(await c.Borrar(baja.Id, img.Id, default));
        Assert.Single(db.HabitacionImagenes);
    }

    [Fact]
    public async Task Reordenar_asigna_el_nuevo_orden()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var a = Datos.Imagen(db, h.Id, 0); var b = Datos.Imagen(db, h.Id, 1); var c = Datos.Imagen(db, h.Id, 2);

        var r = await Crear(db, st).Reordenar(h.Id, new OrdenImagenesDto { Ids = [c.Id, a.Id, b.Id] }, default);

        var lista = Assert.IsAssignableFrom<IEnumerable<ImagenVista>>(Assert.IsType<OkObjectResult>(r).Value).ToList();
        Assert.Equal(new[] { c.Id, a.Id, b.Id }, lista.Select(i => i.Id));
        Assert.Equal(new[] { 0, 1, 2 }, lista.Select(i => i.Orden));
        var enBase = await db.HabitacionImagenes.OrderBy(i => i.Orden).Select(i => i.Id).ToListAsync();
        Assert.Equal(new[] { c.Id, a.Id, b.Id }, enBase);
    }

    // Review Focus 4
    [Theory]
    [InlineData("faltante")]
    [InlineData("repetido")]
    [InlineData("ajeno")]
    [InlineData("sobrante")]
    public async Task Reordenar_con_una_lista_que_no_coincide_es_400_y_no_cambia_nada(string caso)
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var otra = Datos.Habitacion(db); var st = new StorageFalso();
        var a = Datos.Imagen(db, h.Id, 0); var b = Datos.Imagen(db, h.Id, 1);
        var ajena = Datos.Imagen(db, otra.Id, 0);
        List<Guid> ids = caso switch
        {
            "faltante" => [a.Id],
            "repetido" => [a.Id, a.Id],
            "ajeno" => [a.Id, ajena.Id],
            _ => [a.Id, b.Id, Guid.NewGuid()],
        };

        var r = await Crear(db, st).Reordenar(h.Id, new OrdenImagenesDto { Ids = ids }, default);

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal("La lista de imágenes no coincide con las de la habitación.", Datos.Mensaje(r));
        Assert.Equal(0, (await db.HabitacionImagenes.FindAsync(a.Id))!.Orden);
        Assert.Equal(1, (await db.HabitacionImagenes.FindAsync(b.Id))!.Orden);
    }

    [Fact]
    public async Task Reordenar_en_habitacion_inexistente_es_404_y_en_una_dada_de_baja_es_409()
    {
        var db = Datos.NuevoDb(); var baja = Datos.Habitacion(db, activo: false); var st = new StorageFalso();
        var c = Crear(db, st);
        Assert.IsType<NotFoundResult>(await c.Reordenar(Guid.NewGuid(), new OrdenImagenesDto(), default));
        Assert.IsType<ConflictObjectResult>(await c.Reordenar(baja.Id, new OrdenImagenesDto(), default));
    }
}
