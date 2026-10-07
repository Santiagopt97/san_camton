using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservasApi.Models;

public static class EstadosReserva
{
    public static readonly string[] Validos = ["Confirmada", "Check-in", "Check-out", "Cancelada"];
}

[Table("reservas", Schema = "public")]
public class Reserva
{
    [Column("id")] public Guid Id { get; set; }
    [Column("cliente_id")] public Guid ClienteId { get; set; }
    [Column("habitacion_id")] public Guid HabitacionId { get; set; }
    [Column("fecha_entrada")] public DateOnly FechaEntrada { get; set; }
    [Column("fecha_salida")] public DateOnly FechaSalida { get; set; }
    [Column("huespedes")] public int Huespedes { get; set; } = 1;
    [Column("total")] public decimal Total { get; set; }
    [Column("estado")] public string Estado { get; set; } = "Confirmada";
    [Column("notas")] public string? Notas { get; set; }
    [Column("creado_en")] public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

// Vistas de solo lectura de las tablas de otros módulos (misma base de datos)
[Table("clientes", Schema = "public")]
public class ClienteRef
{
    [Column("id")] public Guid Id { get; set; }
    [Column("nombres")] public string Nombres { get; set; } = "";
    [Column("apellidos")] public string Apellidos { get; set; } = "";
    [Column("numero_documento")] public string NumeroDocumento { get; set; } = "";
    [Column("activo")] public bool Activo { get; set; }
}

[Table("habitaciones", Schema = "public")]
public class HabitacionRef
{
    [Column("id")] public Guid Id { get; set; }
    [Column("numero")] public string Numero { get; set; } = "";
    [Column("tipo")] public string Tipo { get; set; } = "";
    [Column("capacidad")] public int Capacidad { get; set; }
    [Column("precio_noche")] public decimal PrecioNoche { get; set; }
    [Column("estado")] public string Estado { get; set; } = "";
    [Column("activo")] public bool Activo { get; set; }
}

[Table("habitacion_imagenes", Schema = "public")]
public class HabitacionImagenRef
{
    [Column("id")] public Guid Id { get; set; }
    [Column("habitacion_id")] public Guid HabitacionId { get; set; }
    [Column("url")] public string Url { get; set; } = "";
    [Column("orden")] public int Orden { get; set; }
    [Column("creado_en")] public DateTime CreadoEn { get; set; }
}

public class ReservaDto
{
    [Required] public Guid ClienteId { get; set; }
    [Required] public Guid HabitacionId { get; set; }
    [Required] public DateOnly FechaEntrada { get; set; }
    [Required] public DateOnly FechaSalida { get; set; }
    [Range(1, 20)] public int Huespedes { get; set; } = 1;
    [MaxLength(500)] public string? Notas { get; set; }
}

public class EstadoDto { [Required] public string Estado { get; set; } = ""; }

public record ReservaVista(Guid Id, Guid ClienteId, Guid HabitacionId, DateOnly FechaEntrada, DateOnly FechaSalida,
    int Huespedes, decimal Total, string Estado, string? Notas, string Cliente, string Habitacion, string Tipo);

public class ReservaHuespedDto
{
    [Required] public Guid HabitacionId { get; set; }
    [Required] public DateOnly FechaEntrada { get; set; }
    [Required] public DateOnly FechaSalida { get; set; }
    [Range(1, 20)] public int Huespedes { get; set; } = 1;
    [MaxLength(500)] public string? Notas { get; set; }
}
