namespace HotelSecurity;

public static class Origenes
{
    // Los 4 fronts: auth-api los necesita todos porque cada módulo le pregunta quién es el usuario (/me)
    public static readonly string[] TodosLosFronts =
    [
        "http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "http://localhost:5176",
    ];

    // Lee una lista separada por comas (variable CORS_ORIGINS); si no hay nada útil, usa `porDefecto`
    public static string[] Parsear(string? valor, string[] porDefecto)
    {
        if (string.IsNullOrWhiteSpace(valor)) return porDefecto;
        var origenes = valor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return origenes.Length > 0 ? origenes : porDefecto;
    }
}
