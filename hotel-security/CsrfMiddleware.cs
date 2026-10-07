using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HotelSecurity;

// Como la cookie viaja sola, las peticiones que modifican datos deben traer una cabecera propia:
// un sitio ajeno no puede añadirla sin pasar antes el control de CORS.
public class CsrfMiddleware(RequestDelegate next)
{
    public const string Cabecera = "X-Requested-With";
    public const string Valor = "hotel-ui";

    private static readonly HashSet<string> Seguros = new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task InvokeAsync(HttpContext ctx)
    {
        var seguro = Seguros.Contains(ctx.Request.Method);
        // Con Authorization: Bearer la petición no depende de la cookie, así que no es falsificable entre sitios
        var conBearer = ctx.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        var conCabecera = ctx.Request.Headers[Cabecera] == Valor;
        if (seguro || conBearer || conCabecera)
        {
            await next(ctx);
            return;
        }
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { message = "Falta la cabecera de seguridad." });
    }
}

public static class CsrfExtensions
{
    public static IApplicationBuilder UseCsrfHeader(this IApplicationBuilder app) => app.UseMiddleware<CsrfMiddleware>();
}
