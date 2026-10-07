namespace HabitacionesApi.Models;

// Lo único que se muestra al público: nunca número de habitación, piso, estado, descripción ni ids
public record ImagenPublica(string Url);

public record HabitacionPublica(string Tipo, int Capacidad, decimal PrecioDesde, List<ImagenPublica> Imagenes);
