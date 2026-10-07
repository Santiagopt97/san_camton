using HabitacionesApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitacionesApi.Data;

public class HabitacionesDbContext(DbContextOptions<HabitacionesDbContext> options) : DbContext(options)
{
    public DbSet<Habitacion> Habitaciones => Set<Habitacion>();
    public DbSet<HabitacionImagen> HabitacionImagenes => Set<HabitacionImagen>();
}
