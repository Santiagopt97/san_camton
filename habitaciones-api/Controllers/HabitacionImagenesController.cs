using HabitacionesApi.Data;
using HabitacionesApi.Models;
using HabitacionesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitacionesApi.Controllers;

[ApiController]
[Authorize(Roles = "admin,recepcion")]
[Route("api/habitaciones/{habitacionId:guid}/imagenes")]
public class HabitacionImagenesController(HabitacionesDbContext db, IImagenStorage storage,
    ILogger<HabitacionImagenesController> log) : ControllerBase
{
    public const int MaxImagenes = 6;
    public const long MaxBytes = 5 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)] // el archivo más el sobrecosto del multipart
    public async Task<IActionResult> Subir(Guid habitacionId, IFormFile? archivo, CancellationToken ct)
    {
        var error = await ValidarHabitacion(habitacionId, ct);
        if (error is not null) return error;
        if (archivo is null) return BadRequest(new { message = "Selecciona una imagen." });
        if (archivo.Length == 0) return BadRequest(new { message = "La imagen está vacía." });
        if (archivo.Length > MaxBytes) return BadRequest(new { message = "La imagen supera 5 MB." });

        await using var contenido = archivo.OpenReadStream();
        var cabecera = new byte[12];
        var leidos = await contenido.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false, ct);
        var tipo = FirmaImagen.Detectar(cabecera.AsSpan(0, leidos));
        if (tipo is null) return BadRequest(new { message = "Formato no permitido. Usa JPG, PNG o WebP." });

        var ordenes = await db.HabitacionImagenes.Where(i => i.HabitacionId == habitacionId)
            .Select(i => i.Orden).ToListAsync(ct);
        if (ordenes.Count >= MaxImagenes)
            return Conflict(new { message = $"Máximo {MaxImagenes} imágenes por habitación." });

        contenido.Position = 0;
        var ruta = $"{habitacionId}/{Guid.NewGuid()}.{tipo.Extension}";
        try { await storage.SubirAsync(ruta, contenido, tipo.ContentType, ct); }
        catch (ImagenStorageException e)
        {
            log.LogError(e, "No se pudo subir la imagen de la habitación {Habitacion}", habitacionId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "No se pudo guardar la imagen. Intenta de nuevo." });
        }

        var img = new HabitacionImagen
        {
            Id = Guid.NewGuid(), HabitacionId = habitacionId, Ruta = ruta, Url = storage.UrlPublica(ruta),
            Orden = ordenes.Count == 0 ? 0 : ordenes.Max() + 1, CreadoEn = DateTime.UtcNow,
        };
        db.HabitacionImagenes.Add(img);
        try { await db.SaveChangesAsync(ct); }
        catch { await IntentarBorrar(ruta); throw; } // no dejar un objeto huérfano en el bucket
        return StatusCode(StatusCodes.Status201Created, new ImagenVista(img.Id, img.Url, img.Orden));
    }

    // 404 si no existe, 409 si está dada de baja; null si se puede modificar
    private async Task<IActionResult?> ValidarHabitacion(Guid id, CancellationToken ct)
    {
        var h = await db.Habitaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();
        if (!h.Activo) return Conflict(new { message = "No se pueden cambiar las imágenes de una habitación dada de baja." });
        return null;
    }

    private async Task IntentarBorrar(string ruta)
    {
        try { await storage.BorrarAsync(ruta, CancellationToken.None); }
        catch (ImagenStorageException e) { log.LogWarning(e, "No se pudo borrar el objeto {Ruta} del bucket", ruta); }
    }
}
