using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace HotelSecurity;

public static class TokenDesdeCookie
{
    // Para JwtBearerEvents.OnMessageReceived: si la petición no trae Authorization, toma el token de la cookie de sesión
    public static Task Leer(MessageReceivedContext contexto)
    {
        if (string.IsNullOrEmpty(contexto.Request.Headers.Authorization)
            && contexto.Request.Cookies.TryGetValue(SesionCookie.Nombre, out var token)
            && !string.IsNullOrWhiteSpace(token))
        {
            contexto.Token = token;
        }
        return Task.CompletedTask;
    }
}
