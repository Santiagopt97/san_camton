using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthApi.Models;

[Table("usuarios", Schema = "public")]
public class Usuario
{
    [Column("id")] public Guid Id { get; set; }
    [Column("email")] public string Email { get; set; } = "";
    [Column("password_hash")] public string PasswordHash { get; set; } = "";
    [Column("nombre")] public string Nombre { get; set; } = "";
    [Column("perfil")] public string Perfil { get; set; } = "recepcion";
    [Column("activo")] public bool Activo { get; set; } = true;
    [Column("cliente_id")] public Guid? ClienteId { get; set; }
}

// Columnas mínimas de la tabla clientes (para el registro de huéspedes)
[Table("clientes", Schema = "public")]
public class ClienteReg
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tipo_documento")] public string TipoDocumento { get; set; } = "CC";
    [Column("numero_documento")] public string NumeroDocumento { get; set; } = "";
    [Column("nombres")] public string Nombres { get; set; } = "";
    [Column("apellidos")] public string Apellidos { get; set; } = "";
    [Column("email")] public string? Email { get; set; }
    [Column("telefono")] public string? Telefono { get; set; }
    [Column("activo")] public bool Activo { get; set; } = true;
    [Column("creado_en")] public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class RegistroDto : IValidatableObject
{
    [Required(ErrorMessage = "El tipo de documento es obligatorio."), RegularExpression("^(CC|CE|TI|PA)$", ErrorMessage = "Tipo de documento no válido.")]
    public string TipoDocumento { get; set; } = "CC";
    [Required(ErrorMessage = "El número de documento es obligatorio."), MaxLength(30)] public string NumeroDocumento { get; set; } = "";
    [Required(ErrorMessage = "Los nombres son obligatorios."), MaxLength(80), RegularExpression(@"^\p{L}[\p{L}\s'.-]*$", ErrorMessage = "Los nombres solo pueden contener letras.")]
    public string Nombres { get; set; } = "";
    [Required(ErrorMessage = "Los apellidos son obligatorios."), MaxLength(80), RegularExpression(@"^\p{L}[\p{L}\s'.-]*$", ErrorMessage = "Los apellidos solo pueden contener letras.")]
    public string Apellidos { get; set; } = "";
    [Required(ErrorMessage = "El correo es obligatorio."), MaxLength(120)] public string Email { get; set; } = "";
    [MaxLength(30)] public string? Telefono { get; set; }
    [Required(ErrorMessage = "La contraseña es obligatoria."),
     RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,72}$", ErrorMessage = "La contraseña debe tener 8 a 72 caracteres, con mayúscula, minúscula y número.")]
    public string Password { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        var doc = NumeroDocumento.Trim();
        var ok = TipoDocumento == "PA" ? Regex.IsMatch(doc, @"^[A-Za-z0-9]{5,20}$") : Regex.IsMatch(doc, @"^\d{5,12}$");
        if (!ok) yield return new ValidationResult("Número de documento no válido para el tipo seleccionado.", [nameof(NumeroDocumento)]);
        if (!Regex.IsMatch(Email.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$"))
            yield return new ValidationResult("El correo no es válido.", [nameof(Email)]);
        if (!string.IsNullOrWhiteSpace(Telefono) && !Regex.IsMatch(Telefono.Trim(), @"^\+?[0-9][0-9\s()-]{6,19}$"))
            yield return new ValidationResult("El teléfono no es válido.", [nameof(Telefono)]);
    }
}

public class LoginDto
{
    [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress(ErrorMessage = "El correo no es válido."), MaxLength(120)] public string Email { get; set; } = "";
    [Required(ErrorMessage = "La contraseña es obligatoria."), MaxLength(72)] public string Password { get; set; } = "";
}
