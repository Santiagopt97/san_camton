using Microsoft.EntityFrameworkCore;
using ReservasApi.Models;

namespace ReservasApi.Data;

public class ReservasDbContext(DbContextOptions<ReservasDbContext> options) : DbContext(options)
{
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<ClienteRef> Clientes => Set<ClienteRef>();
    public DbSet<HabitacionRef> Habitaciones => Set<HabitacionRef>();
    public DbSet<HabitacionImagenRef> HabitacionImagenes => Set<HabitacionImagenRef>();
}
