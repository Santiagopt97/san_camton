using HabitacionesApi.Data;
using HabitacionesApi.Models;
using HabitacionesApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitacionesApi.Tests;

public sealed class StorageFalso : IImagenStorage
{
    public List<string> Subidos { get; } = [];
    public List<string> Borrados { get; } = [];
    public bool FallaAlSubir { get; set; }
    public bool FallaAlBorrar { get; set; }
    public Exception? ExcepcionAlBorrar { get; set; }

    public Task SubirAsync(string ruta, Stream contenido, string contentType, CancellationToken ct)
    {
        if (FallaAlSubir) throw new ImagenStorageException("falló");
        Subidos.Add(ruta);
        return Task.CompletedTask;
    }

    public Task BorrarAsync(string ruta, CancellationToken ct)
    {
        if (ExcepcionAlBorrar is not null) throw ExcepcionAlBorrar;
        if (FallaAlBorrar) throw new ImagenStorageException("falló");
        Borrados.Add(ruta);
        return Task.CompletedTask;
    }

    public string UrlPublica(string ruta) => $"https://cdn.test/{ruta}";
}

// Base de datos que falla al guardar, para probar la compensación
public sealed class DbQueFalla(DbContextOptions<HabitacionesDbContext> o) : HabitacionesDbContext(o)
{
    public bool Falla { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Falla ? throw new InvalidOperationException("fallo de base de datos") : base.SaveChangesAsync(cancellationToken);
}

public static class Datos
{
    public static DbContextOptions<HabitacionesDbContext> Opciones(string? nombre = null) =>
        new DbContextOptionsBuilder<HabitacionesDbContext>().UseInMemoryDatabase(nombre ?? Guid.NewGuid().ToString()).Options;

    public static HabitacionesDbContext NuevoDb() => new(Opciones());

    public static Habitacion Habitacion(HabitacionesDbContext db, bool activo = true)
    {
        var h = new Habitacion { Id = Guid.NewGuid(), Numero = Guid.NewGuid().ToString("N")[..6], Activo = activo };
        db.Habitaciones.Add(h);
        db.SaveChanges();
        return h;
    }

    public static HabitacionImagen Imagen(HabitacionesDbContext db, Guid habitacionId, int orden, string? ruta = null)
    {
        ruta ??= $"{habitacionId}/{Guid.NewGuid()}.png";
        var i = new HabitacionImagen
        {
            Id = Guid.NewGuid(), HabitacionId = habitacionId, Ruta = ruta,
            Url = $"https://cdn.test/{ruta}", Orden = orden, CreadoEn = DateTime.UtcNow.AddSeconds(orden),
        };
        db.HabitacionImagenes.Add(i);
        db.SaveChanges();
        return i;
    }

    public static IFormFile Archivo(byte[] contenido, string nombre = "foto.png", string tipo = "image/png")
    {
        var ms = new MemoryStream(contenido);
        return new FormFile(ms, 0, ms.Length, "archivo", nombre) { Headers = new HeaderDictionary(), ContentType = tipo };
    }

    // PNG mínimo válido en la cabecera, relleno de ceros hasta `total` bytes
    public static byte[] Png(int total = 64)
    {
        var b = new byte[total];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(b, 0);
        return b;
    }

    public static string Mensaje(IActionResult r)
    {
        var valor = ((ObjectResult)r).Value!;
        return (string)valor.GetType().GetProperty("message")!.GetValue(valor)!;
    }
}
