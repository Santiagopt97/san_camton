using HabitacionesApi.Data;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitacionesApi.Controllers;

[ApiController]
[Authorize(Roles = "admin,recepcion")]
[Route("api/habitaciones")]
public class HabitacionesController(HabitacionesDbContext db) : ControllerBase
{
    private const string EstadoOcupada = "Ocupada";
    private const string MensajeHuespedAlojado =
        "No se puede cambiar el estado: la habitación tiene un huésped alojado (check-in). Registra primero el check-out.";

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? q, [FromQuery] string? estado,
        [FromQuery] string? tipo, [FromQuery] bool soloActivas = false,
        [FromQuery] int page = 1, [FromQuery] int size = 12)
    {
        size = Math.Clamp(size, 1, 100);
        var query = db.Habitaciones.AsNoTracking().AsQueryable();
        if (soloActivas) query = query.Where(h => h.Activo);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(h => h.Estado == estado);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(h => h.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var p = $"%{q.Trim()}%";
            query = query.Where(h => EF.Functions.ILike(h.Numero, p) || EF.Functions.ILike(h.Tipo, p)
                || (h.Descripcion != null && EF.Functions.ILike(h.Descripcion, p)));
        }
        var total = await query.CountAsync();
        var items = await query.OrderBy(h => h.Piso).ThenBy(h => h.Numero)
            .Skip((Math.Max(page, 1) - 1) * size).Take(size).ToListAsync();
        await CargarImagenes(items);
        return Ok(new { total, page, size, items });
    }

    // Conteo por estado para el resumen del panel
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        var grupos = await db.Habitaciones.AsNoTracking().Where(h => h.Activo)
            .GroupBy(h => h.Estado).Select(g => new { estado = g.Key, cantidad = g.Count() }).ToListAsync();
        return Ok(EstadosHabitacion.Validos.Select(e => new
        {
            estado = e, cantidad = grupos.FirstOrDefault(g => g.estado == e)?.cantidad ?? 0
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id)
    {
        var h = await db.Habitaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        await CargarImagenes([h]);
        return Ok(h);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(HabitacionDto dto)
    {
        if (!EstadosHabitacion.Validos.Contains(dto.Estado))
            return BadRequest(new { message = "Estado no válido." });
        if (await db.Habitaciones.AnyAsync(h => h.Numero == dto.Numero.Trim()))
            return Conflict(new { message = "Ya existe una habitación con ese número." });
        var h = new Habitacion { Id = Guid.NewGuid(), CreadoEn = DateTime.UtcNow };
        Map(dto, h);
        db.Habitaciones.Add(h);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = h.Id }, h);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, HabitacionDto dto)
    {
        if (!EstadosHabitacion.Validos.Contains(dto.Estado))
            return BadRequest(new { message = "Estado no válido." });
        var h = await db.Habitaciones.FindAsync(id);
        if (h is null) return NotFound();
        if (await db.Habitaciones.AnyAsync(x => x.Numero == dto.Numero.Trim() && x.Id != id))
            return Conflict(new { message = "Ya existe otra habitación con ese número." });
        if (dto.Estado != h.Estado && await HuespedAlojado(h))
            return Conflict(new { message = MensajeHuespedAlojado });
        if (!dto.Activo && h.Activo)
        {
            if (h.Estado == EstadoOcupada || dto.Estado == EstadoOcupada)
                return Conflict(new { message = "No se puede desactivar: la habitación está ocupada." });
            if (await TieneReservasVigentes(id))
                return Conflict(new { message = "No se puede desactivar: la habitación tiene reservas vigentes." });
        }
        Map(dto, h);
        await db.SaveChangesAsync();
        return Ok(h);
    }

    // Cambio rápido de estado (Disponible / Ocupada / Limpieza / Mantenimiento)
    [HttpPatch("{id:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, EstadoDto dto)
    {
        if (!EstadosHabitacion.Validos.Contains(dto.Estado))
            return BadRequest(new { message = "Estado no válido." });
        var h = await db.Habitaciones.FindAsync(id);
        if (h is null) return NotFound();
        if (!h.Activo)
            return Conflict(new { message = "No se puede cambiar el estado de una habitación dada de baja." });
        if (dto.Estado != h.Estado && await HuespedAlojado(h))
            return Conflict(new { message = MensajeHuespedAlojado });
        h.Estado = dto.Estado;
        await db.SaveChangesAsync();
        return Ok(h);
    }

    // Baja lógica; solo el administrador puede dar de baja
    [Authorize(Roles = "admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var h = await db.Habitaciones.FindAsync(id);
        if (h is null) return NotFound();
        if (h.Estado == EstadoOcupada)
            return Conflict(new { message = "No se puede dar de baja: la habitación está ocupada." });
        if (await TieneReservasVigentes(id))
            return Conflict(new { message = "No se puede dar de baja: la habitación tiene reservas vigentes." });
        h.Activo = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Una habitación Ocupada con reserva en Check-in solo se libera con el check-out desde Reservas
    // Rellena `Imagenes` (ordenadas) de las habitaciones dadas, con una sola consulta
    private async Task CargarImagenes(IReadOnlyCollection<Habitacion> habitaciones)
    {
        if (habitaciones.Count == 0) return;
        var ids = habitaciones.Select(h => h.Id).ToList();
        var filas = await db.HabitacionImagenes.AsNoTracking().Where(i => ids.Contains(i.HabitacionId))
            .OrderBy(i => i.Orden).ThenBy(i => i.CreadoEn).ToListAsync();
        var porHabitacion = filas.ToLookup(i => i.HabitacionId, i => new ImagenVista(i.Id, i.Url, i.Orden));
        foreach (var h in habitaciones) h.Imagenes = porHabitacion[h.Id].ToList();
    }

    private async Task<bool> HuespedAlojado(Habitacion h) =>
        h.Estado == EstadoOcupada && await db.Database.SqlQuery<int>($"select count(*)::int as \"Value\" from public.reservas where habitacion_id = {h.Id} and estado = 'Check-in'").SingleAsync() > 0;

    private async Task<bool> TieneReservasVigentes(Guid id) =>
        await db.Database.SqlQuery<int>($"select count(*)::int as \"Value\" from public.reservas where habitacion_id = {id} and estado in ('Confirmada','Check-in') and fecha_salida >= current_date").SingleAsync() > 0;

    private static void Map(HabitacionDto d, Habitacion h)
    {
        h.Numero = d.Numero.Trim(); h.Tipo = d.Tipo; h.Piso = d.Piso; h.Capacidad = d.Capacidad;
        h.PrecioNoche = d.PrecioNoche; h.Estado = d.Estado; h.Descripcion = d.Descripcion; h.Activo = d.Activo;
    }
}
