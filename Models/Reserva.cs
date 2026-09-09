using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace san_camton.Models
{
    public class Reserva
    {
        [Key]
        public int ReservaID { get; set; }
        [Required]
        [ForeignKey(nameof(Cliente))]
        public Guid ClienteID { get; set; }
        [Required]
        [ForeignKey(nameof(Habitacion))]
        public int HabitacionID { get; set; }
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime FechaEntrada { get; set; }
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime FechaSalida { get; set; }
        [Required]
        [EnumDataType(typeof(EstadoReserva))]
        public EstadoReserva EstadoReserva { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El monto total debe ser un valor positivo.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MontoTotal { get; set; }

        public Cliente Cliente { get; set; } = null!;
        public Habitacion Habitacion { get; set; } = null!;
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
        public ICollection<RegistroOperativo> RegistrosOperativos { get; set; } = new List<RegistroOperativo>();
    }
}