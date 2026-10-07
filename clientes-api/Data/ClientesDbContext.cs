using ClientesApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientesApi.Data;

public class ClientesDbContext(DbContextOptions<ClientesDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
}
