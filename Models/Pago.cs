using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace san_camton.Models
{
    public class Pago
    {
        [Key]
        public int PagoID { get; set; }

        [Required]
        [ForeignKey(nameof(Reserva))]
        public int ReservaID { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El monto debe ser un valor positivo.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Monto { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime FechaPago { get; set; }

        [Required]
        [EnumDataType(typeof(MetodoPago))]
        public MetodoPago MetodoPago { get; set; }

        [Required]
        [EnumDataType(typeof(EstadoPago))]
        public EstadoPago EstadoPago { get; set; }

        public Reserva Reserva { get; set; } = null!;
    }
}