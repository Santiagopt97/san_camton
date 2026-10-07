using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasApi.Data;
using ReservasApi.Models;
using ReservasApi.Services;

namespace ReservasApi.Controllers;

[ApiController]
[Authorize(Roles = "admin,recepcion")]
[Route("api/reservas")]
public class ReservasController(ReservasDbContext db, ReservaService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? estado, [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        size = Math.Clamp(size, 1, 100);
        var query = db.Reservas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(r => r.Estado == estado);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var p = $"%{q.Trim()}%";
            query = query.Where(r =>
                db.Clientes.Any(c => c.Id == r.ClienteId && (EF.Functions.ILike(c.Nombres, p) ||
                    EF.Functions.ILike(c.Apellidos, p) || EF.Functions.ILike(c.NumeroDocumento, p))) ||
                db.Habitaciones.Any(h => h.Id == r.HabitacionId && EF.Functions.ILike(h.Numero, p)));
        }
        var total = await query.CountAsync();
        var pagina = query.OrderByDescending(r => r.FechaEntrada).ThenBy(r => r.Id)
            .Skip((Math.Max(page, 1) - 1) * size).Take(size);
        var items = await (
            from r in pagina
            join c in db.Clientes on r.ClienteId equals c.Id
            join h in db.Habitaciones on r.HabitacionId equals h.Id
            orderby r.FechaEntrada descending, r.Id
            select new ReservaVista(r.Id, r.ClienteId, r.HabitacionId, r.FechaEntrada, r.FechaSalida,
                r.Huespedes, r.Total, r.Estado, r.Notas, c.Nombres + " " + c.Apellidos, h.Numero, h.Tipo)
        ).ToListAsync();
        return Ok(new { total, page, size, items });
    }

    // Datos para los selectores del formulario
    [HttpGet("opciones")]
    public async Task<IActionResult> Opciones()
    {
        var clientes = await db.Clientes.AsNoTracking().Where(c => c.Activo).OrderBy(c => c.Apellidos)
            .Select(c => new { c.Id, nombre = c.Nombres + " " + c.Apellidos, c.NumeroDocumento }).ToListAsync();
        var habitaciones = await db.Habitaciones.AsNoTracking().Where(h => h.Activo).OrderBy(h => h.Numero)
            .Select(h => new { h.Id, h.Numero, h.Tipo, h.Capacidad, h.PrecioNoche }).ToListAsync();
        return Ok(new { clientes, habitaciones });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id)
    {
        var r = await db.Reservas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(ReservaDto dto)
    {
        var r = new Reserva { Id = Guid.NewGuid(), CreadoEn = DateTime.UtcNow };
        var (st, msg) = await svc.Aplicar(dto, r, null);
        if (msg is not null) return StatusCode(st, new { message = msg });
        db.Reservas.Add(r);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = r.Id }, r);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ReservaDto dto)
    {
        var r = await db.Reservas.FindAsync(id);
        if (r is null) return NotFound();
        if (r.Estado is "Check-out" or "Cancelada")
            return Conflict(new { message = "No se puede editar una reserva finalizada o cancelada." });
        var (st, msg) = await svc.Aplicar(dto, r, id);
        if (msg is not null) return StatusCode(st, new { message = msg });
        await db.SaveChangesAsync();
        return Ok(r);
    }

    // Flujo permitido: Confirmada → Check-in → Check-out; Confirmada → Cancelada. Check-out y Cancelada son finales.
    private static readonly Dictionary<string, string[]> Transiciones = new()
    {
        ["Confirmada"] = ["Check-in", "Cancelada"],
        ["Check-in"] = ["Check-out"],
        ["Check-out"] = [],
        ["Cancelada"] = [],
    };

    [HttpPatch("{id:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, EstadoDto dto)
    {
        if (!EstadosReserva.Validos.Contains(dto.Estado)) return BadRequest(new { message = "Estado no válido." });
        var r = await db.Reservas.FindAsync(id);
        if (r is null) return NotFound();
        if (r.Estado == dto.Estado) return Ok(r);
        if (!Transiciones[r.Estado].Contains(dto.Estado))
            return Conflict(new { message = $"No se puede pasar de {r.Estado} a {dto.Estado}." });

        var hab = await db.Habitaciones.FindAsync(r.HabitacionId);
        if (dto.Estado == "Check-in")
        {
            if (r.FechaEntrada > DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)))
                return Conflict(new { message = "Aún no es la fecha de entrada de esta reserva." });
            if (hab is null || hab.Estado == "Mantenimiento")
                return Conflict(new { message = "La habitación no está disponible para el check-in." });
            hab.Estado = "Ocupada";
        }
        if (dto.Estado == "Check-out" && hab is not null) hab.Estado = "Limpieza";
        r.Estado = dto.Estado;
        await db.SaveChangesAsync();
        return Ok(r);
    }
}
