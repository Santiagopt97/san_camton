using System.Security.Claims;
using AuthApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Tests;

public class SesionTests
{
    private static LoginDto Login(string email = "ana@hotel.com", string pass = Ayuda.Clave) => new() { Email = email, Password = pass };

    // Review Focus 5
    [Fact]
    public async Task Login_valido_pone_la_cookie_y_responde_204_sin_cuerpo()
    {
        var db = Ayuda.Db(); Ayuda.CrearUsuario(db);
        var (c, http) = Ayuda.Controlador(db);

        var r = await c.Login(Login());

        Assert.IsType<NoContentResult>(r);
        var cookie = Ayuda.SetCookie(http).ToLowerInvariant();
        Assert.Contains("hotel_token=", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=lax", cookie);
        Assert.Contains("path=/", cookie);
        Assert.Contains("expires=", cookie);
        Assert.DoesNotContain("secure", cookie);
    }

    // Review Focus 5
    [Fact]
    public async Task Con_https_la_cookie_de_login_es_secure()
    {
        var db = Ayuda.Db(); Ayuda.CrearUsuario(db);
        var (c, http) = Ayuda.Controlador(db, https: true);
        await c.Login(Login());
        Assert.Contains("secure", Ayuda.SetCookie(http).ToLowerInvariant());
    }

    // Review Focus 5
    [Fact]
    public async Task El_token_lleva_sub_y_role_pero_no_nombre_ni_correo()
    {
        var db = Ayuda.Db(); var u = Ayuda.CrearUsuario(db, perfil: "admin");
        var (c, http) = Ayuda.Controlador(db);
        await c.Login(Login());

        var tipos = Ayuda.TokenDeCookie(http).Claims.Select(x => x.Type).ToList();
        Assert.Contains("sub", tipos);
        Assert.Contains("role", tipos);
        Assert.DoesNotContain("name", tipos);
        Assert.DoesNotContain("email", tipos);
        Assert.DoesNotContain("cliente_id", tipos);
        Assert.Equal(u.Id.ToString(), Ayuda.TokenDeCookie(http).Claims.First(x => x.Type == "sub").Value);
    }

    [Fact]
    public async Task El_token_de_un_huesped_incluye_cliente_id()
    {
        var db = Ayuda.Db(); var cliente = Guid.NewGuid();
        Ayuda.CrearUsuario(db, perfil: "huesped", clienteId: cliente);
        var (c, http) = Ayuda.Controlador(db);
        await c.Login(Login());
        Assert.Equal(cliente.ToString(), Ayuda.TokenDeCookie(http).Claims.First(x => x.Type == "cliente_id").Value);
    }

    [Theory]
    [InlineData("ana@hotel.com", "otra-clave")]
    [InlineData("nadie@hotel.com", Ayuda.Clave)]
    public async Task Credenciales_incorrectas_dan_401_sin_cookie(string email, string pass)
    {
        var db = Ayuda.Db(); Ayuda.CrearUsuario(db);
        var (c, http) = Ayuda.Controlador(db);
        var r = await c.Login(Login(email, pass));
        Assert.IsType<UnauthorizedObjectResult>(r);
        Assert.Equal("Correo o contraseña incorrectos.", Ayuda.Mensaje(r));
        Assert.Equal("", Ayuda.SetCookie(http));
    }

    [Fact]
    public async Task Un_usuario_desactivado_no_inicia_sesion()
    {
        var db = Ayuda.Db(); Ayuda.CrearUsuario(db, activo: false);
        var (c, http) = Ayuda.Controlador(db);
        Assert.IsType<UnauthorizedObjectResult>(await c.Login(Login()));
        Assert.Equal("", Ayuda.SetCookie(http));
    }

    [Fact]
    public async Task Registro_valido_pone_la_cookie_de_huesped_y_responde_204()
    {
        var db = Ayuda.Db();
        var (c, http) = Ayuda.Controlador(db);
        var r = await c.Registro(new RegistroDto
        {
            TipoDocumento = "CC", NumeroDocumento = "12345678", Nombres = "Luis", Apellidos = "Gómez",
            Email = "luis@example.com", Password = Ayuda.Clave,
        });
        Assert.IsType<NoContentResult>(r);
        var claims = Ayuda.TokenDeCookie(http).Claims.ToList();
        Assert.Equal("huesped", claims.First(x => x.Type == "role").Value);
        Assert.Contains(claims, x => x.Type == "cliente_id");
        Assert.DoesNotContain(claims, x => x.Type is "name" or "email");
    }

    [Fact]
    public async Task Me_devuelve_solo_nombre_y_perfil()
    {
        var db = Ayuda.Db(); var u = Ayuda.CrearUsuario(db, perfil: "admin");
        var (c, _) = Ayuda.Controlador(db, usuario: Ayuda.ConSesion(u.Id));

        var r = await c.Me();

        var valor = Assert.IsType<OkObjectResult>(r).Value!;
        var propiedades = valor.GetType().GetProperties().Select(p => p.Name).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "nombre", "perfil" }, propiedades);
        Assert.Equal("Ana Pérez", valor.GetType().GetProperty("nombre")!.GetValue(valor));
        Assert.Equal("admin", valor.GetType().GetProperty("perfil")!.GetValue(valor));
    }

    // Review Focus 3
    [Fact]
    public async Task Me_de_un_usuario_desactivado_o_inexistente_es_401()
    {
        var db = Ayuda.Db(); var u = Ayuda.CrearUsuario(db, activo: false);
        Assert.IsType<UnauthorizedResult>(await Ayuda.Controlador(db, usuario: Ayuda.ConSesion(u.Id)).Controlador.Me());
        Assert.IsType<UnauthorizedResult>(await Ayuda.Controlador(db, usuario: Ayuda.ConSesion(Guid.NewGuid())).Controlador.Me());
    }

    [Fact]
    public async Task Me_sin_claim_sub_es_401()
    {
        var db = Ayuda.Db();
        Assert.IsType<UnauthorizedResult>(await Ayuda.Controlador(db, usuario: Ayuda.ConSesion(null)).Controlador.Me());
    }

    // Review Focus 4
    [Fact]
    public void Logout_responde_204_y_borra_la_cookie_aunque_no_haya_sesion()
    {
        var db = Ayuda.Db();
        var (c, http) = Ayuda.Controlador(db);

        var r = c.Logout();

        Assert.IsType<NoContentResult>(r);
        var cookie = Ayuda.SetCookie(http).ToLowerInvariant();
        Assert.Contains("hotel_token=;", cookie);
        Assert.Contains("expires=thu, 01 jan 1970", cookie);
        Assert.Contains("path=/", cookie);
        Assert.Contains("samesite=lax", cookie);
    }
}
