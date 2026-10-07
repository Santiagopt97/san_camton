using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HabitacionesApi.Data;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace HabitacionesApi.Tests;

// Arranca habitaciones-api de verdad (con base en memoria) para probar el cableado real: cookie, CSRF y CORS
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ClaveJwt = "clave-de-prueba-de-al-menos-32-caracteres-xx";
    private readonly string _bd = Guid.NewGuid().ToString();

    static ApiFactory()
    {
        // Program lee estas variables al arrancar (en la carpeta de pruebas no hay .env)
        void Fijar(string k, string v) => Environment.SetEnvironmentVariable(k, v);
        Fijar("SUPABASE_HOST", "localhost"); Fijar("SUPABASE_USER", "prueba"); Fijar("SUPABASE_PASSWORD", "prueba");
        Fijar("JWT_KEY", ClaveJwt);
        Fijar("SUPABASE_URL", "https://prueba.supabase.co"); Fijar("SUPABASE_SECRET_KEY", "prueba");
        Fijar("CORS_ORIGINS", "http://localhost:5174,http://localhost:5177");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(s =>
        {
            s.RemoveAll<DbContextOptions<HabitacionesDbContext>>();
            s.AddDbContext<HabitacionesDbContext>(o => o.UseInMemoryDatabase(_bd));
        });

    public Guid CrearHabitacion()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HabitacionesDbContext>();
        var h = new Habitacion { Id = Guid.NewGuid(), Numero = Guid.NewGuid().ToString("N")[..6] };
        db.Habitaciones.Add(h);
        db.SaveChanges();
        return h.Id;
    }

    public static string Token(string rol = "admin", bool vencido = false)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ClaveJwt));
        var ahora = DateTime.UtcNow;
        var jwt = new JwtSecurityToken("hotel-auth", "hotel-apis",
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", rol)],
            notBefore: ahora.AddMinutes(vencido ? -120 : -1), expires: ahora.AddMinutes(vencido ? -60 : 60),
            signingCredentials: new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

public class CookieYCsrfTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    private readonly HttpClient _c;

    public CookieYCsrfTests(ApiFactory f)
    {
        _f = f;
        _c = f.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
    }

    private static HttpRequestMessage Peticion(HttpMethod metodo, string ruta, string? cookie = null, string? bearer = null, bool conCabecera = false, HttpContent? cuerpo = null)
    {
        var r = new HttpRequestMessage(metodo, ruta) { Content = cuerpo };
        if (cookie is not null) r.Headers.Add("Cookie", $"hotel_token={cookie}");
        if (bearer is not null) r.Headers.Add("Authorization", $"Bearer {bearer}");
        if (conCabecera) r.Headers.Add("X-Requested-With", "hotel-ui");
        return r;
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
    private static MultipartFormDataContent Archivo() => new() { { new ByteArrayContent(new byte[] { 1, 2, 3 }), "archivo", "x.png" } };
    private static async Task<string> Mensaje(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.GetProperty("message").GetString()!;

    [Fact]
    public async Task Una_lectura_se_autentica_solo_con_la_cookie()
    {
        var r = await _c.SendAsync(Peticion(HttpMethod.Get, "/api/habitaciones", cookie: ApiFactory.Token()));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Sin_cookie_ni_cabecera_es_401()
    {
        var r = await _c.SendAsync(Peticion(HttpMethod.Get, "/api/habitaciones"));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // Review Focus 2
    [Fact]
    public async Task Una_cookie_vencida_o_manipulada_es_401()
    {
        var vencida = await _c.SendAsync(Peticion(HttpMethod.Get, "/api/habitaciones", cookie: ApiFactory.Token(vencido: true)));
        Assert.Equal(HttpStatusCode.Unauthorized, vencida.StatusCode);
        var manipulada = await _c.SendAsync(Peticion(HttpMethod.Get, "/api/habitaciones", cookie: ApiFactory.Token() + "x"));
        Assert.Equal(HttpStatusCode.Unauthorized, manipulada.StatusCode);
    }

    [Fact]
    public async Task Un_rol_sin_permiso_con_cookie_valida_es_403()
    {
        var r = await _c.SendAsync(Peticion(HttpMethod.Get, "/api/habitaciones", cookie: ApiFactory.Token("huesped")));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    // Review Focus 1
    [Fact]
    public async Task Modificar_con_solo_la_cookie_y_sin_la_cabecera_es_400()
    {
        var id = _f.CrearHabitacion();
        var r = await _c.SendAsync(Peticion(HttpMethod.Patch, $"/api/habitaciones/{id}/estado", cookie: ApiFactory.Token(), cuerpo: Json("""{"estado":"Limpieza"}""")));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Falta la cabecera de seguridad.", await Mensaje(r));
    }

    [Fact]
    public async Task Modificar_con_cookie_y_cabecera_pasa()
    {
        var id = _f.CrearHabitacion();
        var r = await _c.SendAsync(Peticion(HttpMethod.Patch, $"/api/habitaciones/{id}/estado", cookie: ApiFactory.Token(), conCabecera: true, cuerpo: Json("""{"estado":"Limpieza"}""")));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    // Review Focus 1
    [Fact]
    public async Task La_subida_multipart_con_solo_la_cookie_y_sin_la_cabecera_es_400()
    {
        var r = await _c.SendAsync(Peticion(HttpMethod.Post, $"/api/habitaciones/{Guid.NewGuid()}/imagenes", cookie: ApiFactory.Token(), cuerpo: Archivo()));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("Falta la cabecera de seguridad.", await Mensaje(r));
    }

    [Fact]
    public async Task La_subida_multipart_con_cookie_y_cabecera_llega_al_controlador()
    {
        // Habitación inexistente: un 404 prueba que pasó la autenticación y la protección CSRF
        var r = await _c.SendAsync(Peticion(HttpMethod.Post, $"/api/habitaciones/{Guid.NewGuid()}/imagenes", cookie: ApiFactory.Token(), conCabecera: true, cuerpo: Archivo()));
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // Review Focus 1
    [Fact]
    public async Task Con_Authorization_Bearer_no_hace_falta_la_cabecera()
    {
        var r = await _c.SendAsync(Peticion(HttpMethod.Put, $"/api/habitaciones/{Guid.NewGuid()}/imagenes/orden", bearer: ApiFactory.Token(), cuerpo: Json("""{"ids":[]}""")));
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode); // llegó al controlador; no es 400 de CSRF ni 401
    }

    [Fact]
    public async Task El_preflight_de_un_origen_permitido_acepta_credenciales_y_la_cabecera_propia()
    {
        var r = new HttpRequestMessage(HttpMethod.Options, "/api/habitaciones");
        r.Headers.Add("Origin", "http://localhost:5174");
        r.Headers.Add("Access-Control-Request-Method", "PATCH");
        r.Headers.Add("Access-Control-Request-Headers", "x-requested-with,content-type");
        var resp = await _c.SendAsync(r);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.Equal("http://localhost:5174", resp.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", resp.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task El_preflight_de_un_origen_no_permitido_no_recibe_permiso()
    {
        var r = new HttpRequestMessage(HttpMethod.Options, "/api/habitaciones");
        r.Headers.Add("Origin", "http://malo.example");
        r.Headers.Add("Access-Control-Request-Method", "PATCH");
        var resp = await _c.SendAsync(r);
        Assert.False(resp.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
