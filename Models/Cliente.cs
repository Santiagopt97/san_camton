using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace san_camton.Models
{
    public class Cliente
    {
        [Key]
        public Guid ClienteID { get; set; }
        [Required]
        [ForeignKey(nameof(Usuario))]
        public Guid UsuarioID { get; set; }
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;
        [Required]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;
        [EmailAddress]
        [Required]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;
        [StringLength(20)]
        [Phone]
        public string Telefono { get; set; } = string.Empty;
        [StringLength(20)]
        public string DocumentoIdentidad { get; set; } = string.Empty;
        [Required]
        public DateTime FechaRegistro { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}