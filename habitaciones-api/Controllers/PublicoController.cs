using HabitacionesApi.Data;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitacionesApi.Controllers;

// Datos públicos para la landing: sin autenticación y con lo mínimo
[ApiController]
[AllowAnonymous]
[Route("api/publico/habitaciones")]
public class PublicoController(HabitacionesDbContext db) : ControllerBase
{
    public const int MaxImagenes = 6;

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var habitaciones = await db.Habitaciones.AsNoTracking().Where(h => h.Activo)
            .Select(h => new { h.Id, h.Tipo, h.Capacidad, h.PrecioNoche, h.CreadoEn }).ToListAsync(ct);
        var ids = habitaciones.Select(h => h.Id).ToList();
        var imagenes = await db.HabitacionImagenes.AsNoTracking().Where(i => ids.Contains(i.HabitacionId))
            .OrderBy(i => i.Orden).ThenBy(i => i.CreadoEn)
            .Select(i => new { i.HabitacionId, i.Url }).ToListAsync(ct);
        var porHabitacion = imagenes.ToLookup(i => i.HabitacionId, i => new ImagenPublica(i.Url));

        var lista = habitaciones.GroupBy(h => h.Tipo).Select(grupo =>
        {
            var masBaratasPrimero = grupo.OrderBy(h => h.PrecioNoche).ThenBy(h => h.CreadoEn).ToList();
            return new HabitacionPublica(
                grupo.Key,
                grupo.Max(h => h.Capacidad),
                grupo.Min(h => h.PrecioNoche),
                masBaratasPrimero.SelectMany(h => porHabitacion[h.Id]).Take(MaxImagenes).ToList());
        }).OrderBy(x => x.PrecioDesde).ThenBy(x => x.Tipo).ToList();

        return Ok(lista);
    }
}
