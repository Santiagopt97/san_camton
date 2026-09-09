using System.ComponentModel.DataAnnotations;

namespace san_camton.Models
{
    public class Usuario
    {
        [Key]
        public Guid UsuarioID { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        public DateTime FechaCreacion { get; set; }

        [Required]
        [EnumDataType(typeof(RolUsuario))]
        public RolUsuario Rol { get; set; }

        public Cliente? Cliente { get; set; }
        public Empleado? Empleado { get; set; }
    }
}