using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace san_camton.Models
{
    public class TipoHabitacion
    {
        [Key]
        public int TipoHabitacionID { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El precio base debe ser un valor positivo.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PrecioBase { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "La capacidad máxima debe estar entre 1 y 100.")]
        public int CapacidadMax { get; set; }

        public ICollection<Habitacion> Habitaciones { get; set; } = new List<Habitacion>();
    }
}