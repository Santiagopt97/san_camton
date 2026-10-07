using HotelSecurity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace HotelSecurity.Tests;

public class TokenDesdeCookieTests
{
    private static MessageReceivedContext Contexto(Action<HttpRequest> configurar)
    {
        var http = new DefaultHttpContext();
        configurar(http.Request);
        var esquema = new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler));
        return new MessageReceivedContext(http, esquema, new JwtBearerOptions());
    }

    [Fact]
    public async Task Toma_el_token_de_la_cookie_si_no_hay_Authorization()
    {
        var c = Contexto(r => r.Headers.Cookie = "otra=1; hotel_token=el-token");
        await TokenDesdeCookie.Leer(c);
        Assert.Equal("el-token", c.Token);
    }

    [Fact]
    public async Task Si_hay_Authorization_no_toca_el_token()
    {
        var c = Contexto(r => { r.Headers.Authorization = "Bearer de-cabecera"; r.Headers.Cookie = "hotel_token=de-cookie"; });
        await TokenDesdeCookie.Leer(c);
        Assert.Null(c.Token);
    }

    [Fact]
    public async Task Sin_cookie_ni_cabecera_no_hay_token()
    {
        var c = Contexto(_ => { });
        await TokenDesdeCookie.Leer(c);
        Assert.Null(c.Token);
    }

    [Fact]
    public async Task Una_cookie_vacia_se_ignora()
    {
        var c = Contexto(r => r.Headers.Cookie = "hotel_token=");
        await TokenDesdeCookie.Leer(c);
        Assert.Null(c.Token);
    }
}
