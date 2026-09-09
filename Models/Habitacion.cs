using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace san_camton.Models
{
    public class Habitacion
    {
        [Key]
        public int HabitacionID { get; set; }

        [Required]
        [ForeignKey(nameof(TipoHabitacion))]
        public int TipoHabitacionID { get; set; }

        [Required]
        [StringLength(10)]
        public string NumeroHabitacion { get; set; } = string.Empty;

        [Required]
        [Range(0, 100, ErrorMessage = "El piso debe estar entre 0 y 100.")]
        public int Piso { get; set; }

        [Required]
        [EnumDataType(typeof(EstadoOperativo))]
        public EstadoOperativo EstadoOperativo { get; set; }

        public TipoHabitacion TipoHabitacion { get; set; } = null!;
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
        public ICollection<RegistroOperativo> RegistrosOperativos { get; set; } = new List<RegistroOperativo>();
    }
}