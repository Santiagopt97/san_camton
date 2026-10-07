using System.Text.Json;
using HotelSecurity;
using Microsoft.AspNetCore.Http;

namespace HotelSecurity.Tests;

public class CsrfMiddlewareTests
{
    private static async Task<(DefaultHttpContext Ctx, bool Paso, string Cuerpo)> Ejecutar(string metodo, Action<HttpRequest>? configurar = null)
    {
        var paso = false;
        var mw = new CsrfMiddleware(_ => { paso = true; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = metodo;
        ctx.Response.Body = new MemoryStream();
        configurar?.Invoke(ctx.Request);
        await mw.InvokeAsync(ctx);
        ctx.Response.Body.Position = 0;
        return (ctx, paso, await new StreamReader(ctx.Response.Body).ReadToEndAsync());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    public async Task Los_metodos_de_lectura_pasan_sin_cabecera(string metodo)
    {
        var (_, paso, _) = await Ejecutar(metodo);
        Assert.True(paso);
    }

    // Review Focus 1
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Los_metodos_que_modifican_sin_cabecera_dan_400(string metodo)
    {
        var (ctx, paso, cuerpo) = await Ejecutar(metodo, r => r.Headers.Cookie = "hotel_token=x");
        Assert.False(paso);
        Assert.Equal(400, ctx.Response.StatusCode);
        Assert.Equal("Falta la cabecera de seguridad.", JsonDocument.Parse(cuerpo).RootElement.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Con_la_cabecera_correcta_pasan(string metodo)
    {
        var (_, paso, _) = await Ejecutar(metodo, r => r.Headers["X-Requested-With"] = "hotel-ui");
        Assert.True(paso);
    }

    [Fact]
    public async Task Una_cabecera_con_otro_valor_no_sirve()
    {
        var (ctx, paso, _) = await Ejecutar("POST", r => r.Headers["X-Requested-With"] = "XMLHttpRequest");
        Assert.False(paso);
        Assert.Equal(400, ctx.Response.StatusCode);
    }

    // Review Focus 1
    [Fact]
    public async Task Con_Authorization_Bearer_pasa_sin_la_cabecera()
    {
        var (_, paso, _) = await Ejecutar("POST", r => r.Headers.Authorization = "Bearer abc");
        Assert.True(paso);
    }

    [Fact]
    public async Task Otro_esquema_de_Authorization_no_exime()
    {
        var (_, paso, _) = await Ejecutar("POST", r => r.Headers.Authorization = "Basic abc");
        Assert.False(paso);
    }
}
