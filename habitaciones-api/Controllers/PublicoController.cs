using HabitacionesApi.Data;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HabitacionesApi.Controllers;

// Datos públicos para la landing: sin autenticación y con lo mínimo.
// CORS propio ("publico"): solo GET y sin credenciales, para que la landing no pueda tocar las rutas con sesión.
[ApiController]
[AllowAnonymous]
[EnableCors("publico")]
[Route("api/publico/habitaciones")]
public class PublicoController(HabitacionesDbContext db, IMemoryCache cache) : ControllerBase
{
    public const int MaxImagenes = 6;
    private const string ClaveCache = "publico:habitaciones";
    private static readonly TimeSpan Vigencia = TimeSpan.FromSeconds(60);

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByHeader = "Origin")]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        // La lista se calcula como mucho una vez por minuto, hagan lo que hagan los clientes con las cabeceras de caché
        var lista = await cache.GetOrCreateAsync(ClaveCache, async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = Vigencia;
            return await Construir(ct);
        });
        return Ok(lista);
    }

    private async Task<List<HabitacionPublica>> Construir(CancellationToken ct)
    {
        var habitaciones = await db.Habitaciones.AsNoTracking().Where(h => h.Activo)
            .Select(h => new { h.Id, h.Tipo, h.Capacidad, h.PrecioNoche, h.CreadoEn }).ToListAsync(ct);
        if (habitaciones.Count == 0) return [];

        var ids = habitaciones.Select(h => h.Id).ToList();
        var imagenes = await db.HabitacionImagenes.AsNoTracking().Where(i => ids.Contains(i.HabitacionId))
            .OrderBy(i => i.Orden).ThenBy(i => i.CreadoEn)
            .Select(i => new { i.HabitacionId, i.Url }).ToListAsync(ct);
        var porHabitacion = imagenes.ToLookup(i => i.HabitacionId, i => new ImagenPublica(i.Url));

        return habitaciones.GroupBy(h => h.Tipo).Select(grupo =>
        {
            var masBaratasPrimero = grupo.OrderBy(h => h.PrecioNoche).ThenBy(h => h.CreadoEn).ToList();
            return new HabitacionPublica(
                grupo.Key,
                grupo.Max(h => h.Capacidad),
                grupo.Min(h => h.PrecioNoche),
                masBaratasPrimero.SelectMany(h => porHabitacion[h.Id]).Take(MaxImagenes).ToList());
        }).OrderBy(x => x.PrecioDesde).ThenBy(x => x.Tipo).ToList();
    }
}
