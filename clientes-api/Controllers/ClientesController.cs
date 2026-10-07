using ClientesApi.Data;
using ClientesApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientesApi.Controllers;

[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin,recepcion")]
[Route("api/clientes")]
public class ClientesController(ClientesDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? q, [FromQuery] bool soloActivos = false,
        [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var query = db.Clientes.AsNoTracking().AsQueryable();
        if (soloActivos) query = query.Where(c => c.Activo);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var p = $"%{q.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.Nombres, p) || EF.Functions.ILike(c.Apellidos, p)
                || EF.Functions.ILike(c.NumeroDocumento, p) || (c.Email != null && EF.Functions.ILike(c.Email, p)));
        }
        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.Apellidos).ThenBy(c => c.Nombres)
            .Skip((Math.Max(page, 1) - 1) * size).Take(size).ToListAsync();
        return Ok(new { total, page, size, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id)
    {
        var c = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(ClienteDto dto)
    {
        if (await db.Clientes.AnyAsync(c => c.NumeroDocumento == dto.NumeroDocumento.Trim()))
            return Conflict(new { message = "Ya existe un cliente con ese número de documento." });
        if (await EmailEnUso(dto.Email, null))
            return Conflict(new { message = "Ya existe un cliente con ese correo." });
        var c = new Cliente { Id = Guid.NewGuid(), CreadoEn = DateTime.UtcNow };
        Map(dto, c);
        db.Clientes.Add(c);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = c.Id }, c);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, ClienteDto dto)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        if (await db.Clientes.AnyAsync(x => x.NumeroDocumento == dto.NumeroDocumento.Trim() && x.Id != id))
            return Conflict(new { message = "Ya existe otro cliente con ese número de documento." });
        if (await EmailEnUso(dto.Email, id))
            return Conflict(new { message = "Ya existe otro cliente con ese correo." });
        if (!dto.Activo && c.Activo && await TieneReservasVigentes(id))
            return Conflict(new { message = "No se puede desactivar: el cliente tiene reservas vigentes." });
        Map(dto, c);
        await db.SaveChangesAsync();
        return Ok(c);
    }

    // Baja lógica: conserva el historial de reservas del cliente.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        if (await TieneReservasVigentes(id))
            return Conflict(new { message = "No se puede desactivar: el cliente tiene reservas vigentes." });
        c.Activo = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> EmailEnUso(string? email, Guid? excluir)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var e = email.Trim().ToLower();
        return await db.Clientes.AnyAsync(x => x.Email != null && x.Email.ToLower() == e && x.Id != excluir);
    }

    private async Task<bool> TieneReservasVigentes(Guid id) =>
        await db.Database.SqlQuery<int>($"select count(*)::int as \"Value\" from public.reservas where cliente_id = {id} and estado in ('Confirmada','Check-in') and fecha_salida >= current_date").SingleAsync() > 0;

    private static string? N(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static void Map(ClienteDto d, Cliente c)
    {
        c.TipoDocumento = d.TipoDocumento; c.NumeroDocumento = d.NumeroDocumento.Trim();
        c.Nombres = d.Nombres.Trim(); c.Apellidos = d.Apellidos.Trim();
        c.Email = N(d.Email)?.ToLower(); c.Telefono = N(d.Telefono); c.Nacionalidad = N(d.Nacionalidad);
        c.FechaNacimiento = d.FechaNacimiento; c.Direccion = N(d.Direccion); c.Activo = d.Activo;
    }
}
