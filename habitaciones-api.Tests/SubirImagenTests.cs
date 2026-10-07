using System.Text;
using HabitacionesApi.Controllers;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HabitacionesApi.Tests;

public class SubirImagenTests
{
    private static HabitacionImagenesController Crear(Data.HabitacionesDbContext db, StorageFalso st) =>
        new(db, st, NullLogger<HabitacionImagenesController>.Instance);

    [Fact]
    public async Task Sube_un_png_valido_y_guarda_la_url()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default);

        var creado = Assert.IsType<ObjectResult>(r);
        Assert.Equal(201, creado.StatusCode);
        var vista = Assert.IsType<ImagenVista>(creado.Value);
        Assert.Equal(0, vista.Orden);
        var ruta = Assert.Single(st.Subidos);
        Assert.StartsWith($"{h.Id}/", ruta);
        Assert.EndsWith(".png", ruta);
        var fila = await db.HabitacionImagenes.SingleAsync();
        Assert.Equal(st.UrlPublica(ruta), fila.Url);
        Assert.Equal(ruta, fila.Ruta);
    }

    [Fact]
    public async Task La_segunda_imagen_queda_al_final()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        Datos.Imagen(db, h.Id, 0);
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default);
        Assert.Equal(1, Assert.IsType<ImagenVista>(((ObjectResult)r).Value).Orden);
    }

    [Fact]
    public async Task Usa_el_tipo_real_y_no_el_declarado()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png(), "cosa.txt", "text/plain"), default);
        Assert.Equal(201, ((ObjectResult)r).StatusCode);
        Assert.EndsWith(".png", Assert.Single(st.Subidos));
    }

    [Fact]
    public async Task Sin_archivo_es_400()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db);
        var r = await Crear(db, new StorageFalso()).Subir(h.Id, null, default);
        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal("Selecciona una imagen.", Datos.Mensaje(r));
    }

    [Fact]
    public async Task Archivo_vacio_es_400()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo([]), default);
        Assert.Equal("La imagen está vacía.", Datos.Mensaje(r));
        Assert.Empty(st.Subidos);
    }

    [Fact]
    public async Task Mas_de_5_MB_es_400_y_exactamente_5_MB_se_permite()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var c = Crear(db, st);
        var grande = await c.Subir(h.Id, Datos.Archivo(Datos.Png((int)HabitacionImagenesController.MaxBytes + 1)), default);
        Assert.IsType<BadRequestObjectResult>(grande);
        Assert.Equal("La imagen supera 5 MB.", Datos.Mensaje(grande));
        Assert.Empty(st.Subidos);

        var justa = await c.Subir(h.Id, Datos.Archivo(Datos.Png((int)HabitacionImagenesController.MaxBytes)), default);
        Assert.Equal(201, ((ObjectResult)justa).StatusCode);
    }

    // Review Focus 1
    [Fact]
    public async Task Rechaza_contenido_que_no_es_imagen_aunque_diga_png()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var falso = Datos.Archivo(Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"), "foto.png", "image/png");
        var r = await Crear(db, st).Subir(h.Id, falso, default);
        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal("Formato no permitido. Usa JPG, PNG o WebP.", Datos.Mensaje(r));
        Assert.Empty(st.Subidos);
        Assert.Empty(db.HabitacionImagenes);
    }

    [Fact]
    public async Task La_septima_imagen_es_409()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        for (var i = 0; i < HabitacionImagenesController.MaxImagenes; i++) Datos.Imagen(db, h.Id, i);
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default);
        Assert.IsType<ConflictObjectResult>(r);
        Assert.Equal("Máximo 6 imágenes por habitación.", Datos.Mensaje(r));
        Assert.Empty(st.Subidos);
    }

    // Review Focus 5
    [Fact]
    public async Task Habitacion_inexistente_es_404_y_dada_de_baja_es_409()
    {
        var db = Datos.NuevoDb(); var baja = Datos.Habitacion(db, activo: false); var st = new StorageFalso();
        var c = Crear(db, st);
        Assert.IsType<NotFoundResult>(await c.Subir(Guid.NewGuid(), Datos.Archivo(Datos.Png()), default));
        var r = await c.Subir(baja.Id, Datos.Archivo(Datos.Png()), default);
        Assert.IsType<ConflictObjectResult>(r);
        Assert.Equal("No se pueden cambiar las imágenes de una habitación dada de baja.", Datos.Mensaje(r));
        Assert.Empty(st.Subidos);
    }

    // Review Focus 2
    [Fact]
    public async Task Si_Storage_falla_es_502_y_no_queda_fila()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso { FallaAlSubir = true };
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default);
        Assert.Equal(502, ((ObjectResult)r).StatusCode);
        Assert.Equal("No se pudo guardar la imagen. Intenta de nuevo.", Datos.Mensaje(r));
        Assert.Empty(db.HabitacionImagenes);
    }

    // Review Focus 2
    [Fact]
    public async Task Si_falla_la_base_despues_de_subir_se_borra_el_objeto()
    {
        var opciones = Datos.Opciones();
        var db = new DbQueFalla(opciones);
        var h = Datos.Habitacion(db);
        var st = new StorageFalso();
        db.Falla = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default));
        Assert.Equal(st.Subidos, st.Borrados);
        Assert.Single(st.Borrados);
    }

    [Fact]
    public void Solo_admin_y_recepcion_pueden_gestionar_imagenes()
    {
        var atributo = typeof(HabitacionImagenesController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Equal("admin,recepcion", atributo.Roles);
    }
}
