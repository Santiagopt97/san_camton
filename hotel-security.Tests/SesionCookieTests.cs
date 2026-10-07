using HotelSecurity;
using Microsoft.AspNetCore.Http;

namespace HotelSecurity.Tests;

public class SesionCookieTests
{
    private static string SetCookie(HttpContext ctx) => ctx.Response.Headers.SetCookie.ToString().ToLowerInvariant();

    [Fact]
    public void Poner_crea_una_cookie_httponly_lax_sin_secure_en_http()
    {
        var ctx = new DefaultHttpContext();
        SesionCookie.Poner(ctx.Response, "abc", DateTimeOffset.UtcNow.AddMinutes(120));
        var c = SetCookie(ctx);
        Assert.Contains("hotel_token=abc", c);
        Assert.Contains("httponly", c);
        Assert.Contains("samesite=lax", c);
        Assert.Contains("path=/", c);
        Assert.Contains("expires=", c);
        Assert.DoesNotContain("secure", c);
        Assert.DoesNotContain("domain=", c);
    }

    // Review Focus 5
    [Fact]
    public void Poner_agrega_secure_solo_con_https()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        SesionCookie.Poner(ctx.Response, "abc", DateTimeOffset.UtcNow.AddMinutes(120));
        Assert.Contains("secure", SetCookie(ctx));
    }

    // Review Focus 4
    [Fact]
    public void Borrar_expira_la_cookie_con_los_mismos_atributos()
    {
        var ctx = new DefaultHttpContext();
        SesionCookie.Borrar(ctx.Response);
        var c = SetCookie(ctx);
        Assert.Contains("hotel_token=;", c);
        Assert.Contains("expires=thu, 01 jan 1970", c);
        Assert.Contains("path=/", c);
        Assert.Contains("samesite=lax", c);
        Assert.Contains("httponly", c);
    }

    [Fact]
    public void Borrar_con_https_conserva_secure()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        SesionCookie.Borrar(ctx.Response);
        Assert.Contains("secure", SetCookie(ctx));
    }
}
