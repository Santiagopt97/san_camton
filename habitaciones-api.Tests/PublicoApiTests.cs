using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HabitacionesApi.Tests;

public class PublicoApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _c;

    public PublicoApiTests(ApiFactory f)
    {
        f.CrearHabitacion();
        _c = f.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
    }

    private static HttpRequestMessage Get(string cookie = null!)
    {
        var r = new HttpRequestMessage(HttpMethod.Get, "/api/publico/habitaciones");
        if (cookie is not null) r.Headers.Add("Cookie", $"hotel_token={cookie}");
        return r;
    }

    // Review Focus 1
    [Fact]
    public async Task Responde_200_sin_credenciales_con_solo_los_campos_publicos()
    {
        var r = await _c.SendAsync(Get());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var raiz = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Array, raiz.ValueKind);
        Assert.Equal(new[] { "capacidad", "imagenes", "precioDesde", "tipo" }, raiz[0].EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public async Task La_respuesta_se_puede_guardar_en_cache_60_segundos()
    {
        var r = await _c.SendAsync(Get());
        Assert.True(r.Headers.CacheControl!.Public);
        Assert.Equal(TimeSpan.FromSeconds(60), r.Headers.CacheControl.MaxAge);
    }

    // Review Focus 5
    [Fact]
    public async Task Una_cookie_vencida_no_impide_ver_la_landing()
    {
        var r = await _c.SendAsync(Get(ApiFactory.Token(vencido: true)));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task El_preflight_del_origen_de_la_landing_se_acepta()
    {
        var r = new HttpRequestMessage(HttpMethod.Options, "/api/publico/habitaciones");
        r.Headers.Add("Origin", "http://localhost:5177");
        r.Headers.Add("Access-Control-Request-Method", "GET");
        var resp = await _c.SendAsync(r);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.Equal("http://localhost:5177", resp.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
