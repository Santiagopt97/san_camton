using System.Net;
using System.Net.Http.Headers;

namespace HabitacionesApi.Services;

public record StorageOptions(string Url, string SecretKey, string Bucket)
{
    // `leer` devuelve el valor de una variable de entorno (o null)
    public static StorageOptions Desde(Func<string, string?> leer)
    {
        var url = leer("SUPABASE_URL");
        var clave = leer("SUPABASE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(clave))
            throw new InvalidOperationException(
                "Faltan SUPABASE_URL o SUPABASE_SECRET_KEY (Storage de imágenes). Copia .env.example como .env y complétalo.");
        var bucket = leer("SUPABASE_BUCKET");
        return new StorageOptions(url.Trim().TrimEnd('/'), clave.Trim(), string.IsNullOrWhiteSpace(bucket) ? "habitaciones" : bucket.Trim());
    }
}

// Cliente mínimo de la API REST de Supabase Storage. La clave secreta solo viaja del backend a Supabase.
public class SupabaseStorage(HttpClient http, StorageOptions opciones, ILogger<SupabaseStorage> log) : IImagenStorage
{
    public string UrlPublica(string ruta) => $"{opciones.Url}/storage/v1/object/public/{opciones.Bucket}/{ruta}";

    private string UrlObjeto(string ruta) => $"{opciones.Url}/storage/v1/object/{opciones.Bucket}/{ruta}";

    private HttpRequestMessage Peticion(HttpMethod metodo, string ruta)
    {
        var req = new HttpRequestMessage(metodo, UrlObjeto(ruta));
        req.Headers.Add("apikey", opciones.SecretKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opciones.SecretKey);
        return req;
    }

    public async Task SubirAsync(string ruta, Stream contenido, string contentType, CancellationToken ct)
    {
        using var req = Peticion(HttpMethod.Post, ruta);
        req.Content = new StreamContent(contenido);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var res = await Enviar(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            log.LogError("Storage rechazó la subida de {Ruta}: {Estado}", ruta, (int)res.StatusCode);
            throw new ImagenStorageException($"Storage respondió {(int)res.StatusCode} al subir la imagen.");
        }
    }

    public async Task BorrarAsync(string ruta, CancellationToken ct)
    {
        using var req = Peticion(HttpMethod.Delete, ruta);
        using var res = await Enviar(req, ct);
        if (res.StatusCode == HttpStatusCode.NotFound) return; // ya no existe: el resultado buscado
        if (!res.IsSuccessStatusCode)
        {
            log.LogWarning("Storage rechazó el borrado de {Ruta}: {Estado}", ruta, (int)res.StatusCode);
            throw new ImagenStorageException($"Storage respondió {(int)res.StatusCode} al borrar la imagen.");
        }
    }

    private async Task<HttpResponseMessage> Enviar(HttpRequestMessage req, CancellationToken ct)
    {
        try { return await http.SendAsync(req, ct); }
        catch (HttpRequestException e) { throw new ImagenStorageException("No se pudo conectar con Storage.", e); }
    }
}
