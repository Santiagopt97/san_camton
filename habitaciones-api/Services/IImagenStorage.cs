namespace HabitacionesApi.Services;

public interface IImagenStorage
{
    Task SubirAsync(string ruta, Stream contenido, string contentType, CancellationToken ct);
    // No falla si el objeto ya no existe
    Task BorrarAsync(string ruta, CancellationToken ct);
    string UrlPublica(string ruta);
}

public class ImagenStorageException(string message, Exception? inner = null) : Exception(message, inner);
