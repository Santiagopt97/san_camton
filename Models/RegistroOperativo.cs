using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace san_camton.Models
{
    public class RegistroOperativo
    {
        [Key]
        public int RegistroID { get; set; }

        [Required]
        [ForeignKey(nameof(Habitacion))]
        public int HabitacionID { get; set; }

        [Required]
        [ForeignKey(nameof(Reserva))]
        public int ReservaID { get; set; }

        [Required]
        [ForeignKey(nameof(Cliente))]
        public Guid ClienteID { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime FechaCheckIn { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? FechaCheckOut { get; set; }

        [Required]
        [ForeignKey(nameof(Recepcionista))]
        public int RecepcionistaID { get; set; }

        [StringLength(1000)]
        public string Observaciones { get; set; } = string.Empty;

        public Habitacion Habitacion { get; set; } = null!;
        public Reserva Reserva { get; set; } = null!;
        public Cliente Cliente { get; set; } = null!;
        public Empleado Recepcionista { get; set; } = null!;
    }
}