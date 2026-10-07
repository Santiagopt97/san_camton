using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasApi.Data;
using ReservasApi.Models;
using ReservasApi.Services;

namespace ReservasApi.Controllers;

// Portal del huésped: solo ve y gestiona sus propias reservas (el cliente sale del token, nunca del cliente HTTP)
[ApiController]
[Authorize(Roles = "huesped")]
[Route("api/mis-reservas")]
public class MisReservasController(ReservasDbContext db, ReservaService svc) : ControllerBase
{
    private Guid? ClienteId => Guid.TryParse(User.FindFirstValue("cliente_id"), out var g) ? g : null;

    [HttpGet("disponibles")]
    public async Task<IActionResult> Disponibles([FromQuery] DateOnly entrada, [FromQuery] DateOnly salida, [FromQuery] int huespedes = 1)
    {
        if (salida <= entrada) return BadRequest(new { message = "La salida debe ser posterior a la entrada." });
        if (entrada < DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)))
            return BadRequest(new { message = "La fecha de entrada no puede ser pasada." });
        var noches = salida.DayNumber - entrada.DayNumber;
        var libres = await db.Habitaciones.AsNoTracking()
            .Where(h => h.Activo && h.Estado != "Mantenimiento" && h.Capacidad >= huespedes)
            .Where(h => !db.Reservas.Any(x => x.HabitacionId == h.Id && x.Estado != "Cancelada" && x.Estado != "Check-out"
                && x.FechaEntrada < salida && x.FechaSalida > entrada))
            .OrderBy(h => h.PrecioNoche)
            .Select(h => new { h.Id, h.Numero, h.Tipo, h.Capacidad, h.PrecioNoche, noches, total = h.PrecioNoche * noches })
            .ToListAsync();
        return Ok(libres);
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        if (ClienteId is null) return Forbid();
        var items = await (
            from r in db.Reservas.AsNoTracking().Where(x => x.ClienteId == ClienteId)
            join c in db.Clientes on r.ClienteId equals c.Id
            join h in db.Habitaciones on r.HabitacionId equals h.Id
            orderby r.FechaEntrada descending
            select new ReservaVista(r.Id, r.ClienteId, r.HabitacionId, r.FechaEntrada, r.FechaSalida,
                r.Huespedes, r.Total, r.Estado, r.Notas, c.Nombres + " " + c.Apellidos, h.Numero, h.Tipo)
        ).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(ReservaHuespedDto dto)
    {
        if (ClienteId is not Guid cid) return Forbid();
        var d = new ReservaDto { ClienteId = cid, HabitacionId = dto.HabitacionId, FechaEntrada = dto.FechaEntrada,
            FechaSalida = dto.FechaSalida, Huespedes = dto.Huespedes, Notas = dto.Notas };
        var r = new Reserva { Id = Guid.NewGuid(), CreadoEn = DateTime.UtcNow, Estado = "Confirmada" };
        var (st, msg) = await svc.Aplicar(d, r, null, esHuesped: true);
        if (msg is not null) return StatusCode(st, new { message = msg });
        db.Reservas.Add(r);
        await db.SaveChangesAsync();
        return Ok(r);
    }

    [HttpPatch("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        if (ClienteId is not Guid cid) return Forbid();
        var r = await db.Reservas.FirstOrDefaultAsync(x => x.Id == id && x.ClienteId == cid);
        if (r is null) return NotFound();
        if (r.Estado != "Confirmada") return Conflict(new { message = "Solo se pueden cancelar reservas confirmadas." });
        if (r.FechaEntrada <= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)))
            return Conflict(new { message = "Ya no se puede cancelar: la estadía comienza hoy o ya comenzó." });
        r.Estado = "Cancelada";
        await db.SaveChangesAsync();
        return Ok(r);
    }
}
