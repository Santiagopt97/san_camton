using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientesApi.Models;

[Table("clientes", Schema = "public")]
public class Cliente
{
    [Column("id")] public Guid Id { get; set; }
    [Column("tipo_documento")] public string TipoDocumento { get; set; } = "CC";
    [Column("numero_documento")] public string NumeroDocumento { get; set; } = "";
    [Column("nombres")] public string Nombres { get; set; } = "";
    [Column("apellidos")] public string Apellidos { get; set; } = "";
    [Column("email")] public string? Email { get; set; }
    [Column("telefono")] public string? Telefono { get; set; }
    [Column("nacionalidad")] public string? Nacionalidad { get; set; }
    [Column("fecha_nacimiento")] public DateOnly? FechaNacimiento { get; set; }
    [Column("direccion")] public string? Direccion { get; set; }
    [Column("activo")] public bool Activo { get; set; } = true;
    [Column("creado_en")] public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class ClienteDto : IValidatableObject
{
    [Required(ErrorMessage = "El tipo de documento es obligatorio."), RegularExpression("^(CC|CE|TI|PA|NIT)$", ErrorMessage = "Tipo de documento no válido.")]
    public string TipoDocumento { get; set; } = "CC";
    [Required(ErrorMessage = "El número de documento es obligatorio."), MaxLength(30)] public string NumeroDocumento { get; set; } = "";
    [Required(ErrorMessage = "Los nombres son obligatorios."), MaxLength(80), RegularExpression(@"^\p{L}[\p{L}\s'.-]*$", ErrorMessage = "Los nombres solo pueden contener letras.")]
    public string Nombres { get; set; } = "";
    [Required(ErrorMessage = "Los apellidos son obligatorios."), MaxLength(80), RegularExpression(@"^\p{L}[\p{L}\s'.-]*$", ErrorMessage = "Los apellidos solo pueden contener letras.")]
    public string Apellidos { get; set; } = "";
    [MaxLength(120)] public string? Email { get; set; }
    [MaxLength(30)] public string? Telefono { get; set; }
    [MaxLength(60)] public string? Nacionalidad { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    [MaxLength(200)] public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        var doc = NumeroDocumento.Trim();
        var ok = TipoDocumento switch
        {
            "CC" or "TI" or "CE" => Regex.IsMatch(doc, @"^\d{5,12}$"),
            "PA" => Regex.IsMatch(doc, @"^[A-Za-z0-9]{5,20}$"),
            "NIT" => Regex.IsMatch(doc, @"^\d{9,10}(-\d)?$"),
            _ => true,
        };
        if (!ok) yield return new ValidationResult("Número de documento no válido para el tipo seleccionado.", [nameof(NumeroDocumento)]);
        if (!string.IsNullOrWhiteSpace(Email) && !Regex.IsMatch(Email.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$"))
            yield return new ValidationResult("El correo no es válido.", [nameof(Email)]);
        if (!string.IsNullOrWhiteSpace(Telefono) && !Regex.IsMatch(Telefono.Trim(), @"^\+?[0-9][0-9\s()-]{6,19}$"))
            yield return new ValidationResult("El teléfono no es válido.", [nameof(Telefono)]);
        if (FechaNacimiento is { } n)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            if (n > hoy) yield return new ValidationResult("La fecha de nacimiento no puede ser futura.", [nameof(FechaNacimiento)]);
            else if (n < hoy.AddYears(-120)) yield return new ValidationResult("La fecha de nacimiento no es válida.", [nameof(FechaNacimiento)]);
            else if (TipoDocumento == "CC" && n > hoy.AddYears(-18))
                yield return new ValidationResult("Con cédula de ciudadanía la persona debe ser mayor de edad.", [nameof(FechaNacimiento)]);
        }
    }
}
