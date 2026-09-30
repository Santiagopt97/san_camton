using Microsoft.EntityFrameworkCore;
using ReservasApi.Data;
using ReservasApi.Models;

namespace ReservasApi.Services;

// Reglas de negocio compartidas por recepción y por el portal del huésped
public class ReservaService(ReservasDbContext db)
{
    // Devuelve (0, null) si todo está bien; si no, (código HTTP, mensaje)
    public async Task<(int Status, string? Message)> Aplicar(ReservaDto d, Reserva r, Guid? excluir, bool esHuesped = false)
    {
        if (d.FechaSalida <= d.FechaEntrada) return (400, "La salida debe ser posterior a la entrada.");
        if (d.FechaSalida.DayNumber - d.FechaEntrada.DayNumber > 30) return (400, "La estadía máxima es de 30 noches.");
        if (esHuesped && d.FechaEntrada > DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)).AddDays(365))
            return (400, "Solo se puede reservar con hasta un año de anticipación.");
        if (esHuesped && d.FechaEntrada < DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)))
            return (400, "La fecha de entrada no puede ser pasada.");
        var cliente = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == d.ClienteId && c.Activo);
        if (cliente is null) return (400, "Cliente no válido o inactivo.");
        var hab = await db.Habitaciones.AsNoTracking().FirstOrDefaultAsync(h => h.Id == d.HabitacionId && h.Activo);
        if (hab is null) return (400, "Habitación no válida o inactiva.");
        if (esHuesped && hab.Estado == "Mantenimiento") return (409, "La habitación no está disponible.");
        if (d.Huespedes > hab.Capacidad) return (400, $"La habitación admite máximo {hab.Capacidad} huésped(es).");
        if (await HayCruce(d.HabitacionId, d.FechaEntrada, d.FechaSalida, excluir))
            return (409, "La habitación ya está reservada en esas fechas.");

        r.ClienteId = d.ClienteId; r.HabitacionId = d.HabitacionId;
        r.FechaEntrada = d.FechaEntrada; r.FechaSalida = d.FechaSalida;
        r.Huespedes = d.Huespedes; r.Notas = d.Notas;
        r.Total = hab.PrecioNoche * (d.FechaSalida.DayNumber - d.FechaEntrada.DayNumber);
        return (0, null);
    }

    public Task<bool> HayCruce(Guid habitacionId, DateOnly entrada, DateOnly salida, Guid? excluir) =>
        db.Reservas.AnyAsync(x => x.HabitacionId == habitacionId && x.Id != excluir
            && x.Estado != "Cancelada" && x.Estado != "Check-out"
            && x.FechaEntrada < salida && x.FechaSalida > entrada);
}
