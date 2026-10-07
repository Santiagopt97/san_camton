using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthApi.Controllers;
using AuthApi.Data;
using AuthApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuthApi.Tests;

public static class Ayuda
{
    public const string Clave = "Clave123*";

    public static AuthDbContext Db() =>
        new(new DbContextOptionsBuilder<AuthDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Key"] = "clave-de-prueba-de-al-menos-32-caracteres-xx",
        ["Jwt:Issuer"] = "hotel-auth",
        ["Jwt:Audience"] = "hotel-apis",
        ["Jwt:ExpiraMinutos"] = "120",
    }).Build();

    public static (AuthController Controlador, DefaultHttpContext Http) Controlador(AuthDbContext db, bool https = false, ClaimsPrincipal? usuario = null)
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = https ? "https" : "http";
        if (usuario is not null) http.User = usuario;
        var c = new AuthController(db, Config()) { ControllerContext = new ControllerContext { HttpContext = http } };
        return (c, http);
    }

    public static Usuario CrearUsuario(AuthDbContext db, string email = "ana@hotel.com", string perfil = "recepcion", bool activo = true, Guid? clienteId = null)
    {
        var u = new Usuario
        {
            Id = Guid.NewGuid(), Email = email, Nombre = "Ana Pérez", Perfil = perfil, Activo = activo, ClienteId = clienteId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Clave),
        };
        db.Usuarios.Add(u);
        db.SaveChanges();
        return u;
    }

    public static ClaimsPrincipal ConSesion(Guid? id) =>
        new(new ClaimsIdentity(id is null ? [] : [new Claim("sub", id.ToString()!), new Claim("role", "recepcion")], "prueba"));

    public static string SetCookie(HttpContext http) => http.Response.Headers.SetCookie.ToString();

    // Lee el token de la cabecera Set-Cookie (nunca se imprime)
    public static JwtSecurityToken TokenDeCookie(HttpContext http)
    {
        var inicio = SetCookie(http).IndexOf("hotel_token=", StringComparison.Ordinal) + "hotel_token=".Length;
        var valor = SetCookie(http)[inicio..].Split(';')[0];
        return new JwtSecurityTokenHandler().ReadJwtToken(Uri.UnescapeDataString(valor));
    }

    public static string Mensaje(IActionResult r)
    {
        var v = ((ObjectResult)r).Value!;
        return (string)v.GetType().GetProperty("message")!.GetValue(v)!;
    }
}
