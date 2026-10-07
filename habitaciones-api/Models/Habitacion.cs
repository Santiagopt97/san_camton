using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HabitacionesApi.Models;

public static class EstadosHabitacion
{
    public static readonly string[] Validos = ["Disponible", "Ocupada", "Limpieza", "Mantenimiento"];
}

[Table("habitaciones", Schema = "public")]
public class Habitacion
{
    [Column("id")] public Guid Id { get; set; }
    [Column("numero")] public string Numero { get; set; } = "";
    [Column("tipo")] public string Tipo { get; set; } = "Sencilla";
    [Column("piso")] public int Piso { get; set; } = 1;
    [Column("capacidad")] public int Capacidad { get; set; } = 1;
    [Column("precio_noche")] public decimal PrecioNoche { get; set; }
    [Column("estado")] public string Estado { get; set; } = "Disponible";
    [Column("descripcion")] public string? Descripcion { get; set; }
    [Column("activo")] public bool Activo { get; set; } = true;
    [Column("creado_en")] public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class HabitacionDto
{
    [Required(ErrorMessage = "El número es obligatorio."), RegularExpression(@"^[A-Za-z0-9-]{1,10}$", ErrorMessage = "El número solo admite letras, números y guion (máx. 10).")] public string Numero { get; set; } = "";
    [Required, RegularExpression("^(Sencilla|Doble|Familiar|Suite)$", ErrorMessage = "Tipo de habitación no válido.")] public string Tipo { get; set; } = "Sencilla";
    [Range(0, 50, ErrorMessage = "El piso debe estar entre 0 y 50.")] public int Piso { get; set; } = 1;
    [Range(1, 10, ErrorMessage = "La capacidad debe estar entre 1 y 10.")] public int Capacidad { get; set; } = 1;
    [Range(typeof(decimal), "1000", "50000000", ErrorMessage = "El precio debe estar entre $1.000 y $50.000.000.")] public decimal PrecioNoche { get; set; }
    [Required] public string Estado { get; set; } = "Disponible";
    [MaxLength(500)] public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public class EstadoDto
{
    [Required] public string Estado { get; set; } = "";
}
