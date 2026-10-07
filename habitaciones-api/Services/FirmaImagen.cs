namespace HabitacionesApi.Services;

// Identifica el formato real de una imagen por sus primeros bytes. No se confía en la extensión ni en el Content-Type.
public static class FirmaImagen
{
    public sealed record Tipo(string ContentType, string Extension);

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // `cabecera` debe traer al menos los primeros 12 bytes del archivo (si el archivo es más corto, los que tenga).
    public static Tipo? Detectar(ReadOnlySpan<byte> cabecera)
    {
        if (cabecera.Length >= 3 && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF)
            return new Tipo("image/jpeg", "jpg");

        if (cabecera.Length >= Png.Length && cabecera[..Png.Length].SequenceEqual(Png))
            return new Tipo("image/png", "png");

        if (cabecera.Length >= 12
            && cabecera[0] == 'R' && cabecera[1] == 'I' && cabecera[2] == 'F' && cabecera[3] == 'F'
            && cabecera[8] == 'W' && cabecera[9] == 'E' && cabecera[10] == 'B' && cabecera[11] == 'P')
            return new Tipo("image/webp", "webp");

        return null;
    }
}
