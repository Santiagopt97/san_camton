using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace san_camton.Models
{
    public class Empleado
    {
        [Key]
        public int EmpleadoID { get; set; }

        [Required]
        [ForeignKey(nameof(Usuario))]
        public Guid UsuarioID { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Cargo { get; set; } = string.Empty;

        [Required]
        public bool Activo { get; set; } = true;

        public Usuario Usuario { get; set; } = null!;
        public ICollection<RegistroOperativo> RegistrosOperativos { get; set; } = new List<RegistroOperativo>();
    }
}