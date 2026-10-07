using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HabitacionesApi.Models;

[Table("habitacion_imagenes", Schema = "public")]
public class HabitacionImagen
{
    [Column("id")] public Guid Id { get; set; }
    [Column("habitacion_id")] public Guid HabitacionId { get; set; }
    [Column("ruta")] public string Ruta { get; set; } = "";
    [Column("url")] public string Url { get; set; } = "";
    [Column("orden")] public int Orden { get; set; }
    [Column("creado_en")] public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

// Lo que ve el cliente de cada imagen
public record ImagenVista(Guid Id, string Url, int Orden);

public class OrdenImagenesDto
{
    [Required] public List<Guid> Ids { get; set; } = [];
}
