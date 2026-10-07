using Microsoft.AspNetCore.Http;

namespace HotelSecurity;

// La sesión viaja en una cookie que el JavaScript de la página no puede leer
public static class SesionCookie
{
    public const string Nombre = "hotel_token";

    private static CookieOptions Opciones(HttpRequest request, DateTimeOffset? expira) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Secure = request.IsHttps, // solo con HTTPS (en desarrollo local es HTTP)
        Expires = expira,
        IsEssential = true,
    };

    public static void Poner(HttpResponse response, string token, DateTimeOffset expira) =>
        response.Cookies.Append(Nombre, token, Opciones(response.HttpContext.Request, expira));

    // Debe usar los mismos atributos (Path, SameSite…) con los que se creó, o el navegador no la borra
    public static void Borrar(HttpResponse response) =>
        response.Cookies.Delete(Nombre, Opciones(response.HttpContext.Request, null));
}
