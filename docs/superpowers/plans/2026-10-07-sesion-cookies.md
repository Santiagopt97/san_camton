# Sesión con cookie HttpOnly Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que la sesión viva en una cookie `HttpOnly` emitida por `auth-api` (sin `sessionStorage` ni token en la URL) y que se exponga lo mínimo del usuario.

**Architecture:** Una biblioteca compartida `hotel-security` (cookie de sesión, lectura del token desde la cookie, middleware anti-CSRF) la usan las 4 APIs. `auth-api` pone la cookie en login y registro (204 sin cuerpo), expone `/me` mínimo y `/logout`. En el navegador, `hotel-ui` guarda `{ nombre, perfil }` solo en memoria (la pide a `/me`) y ofrece un cliente HTTP con credenciales y la cabecera anti-CSRF. `auth-front` usa un contexto de React reactivo; los módulos esperan `initSession()` antes de dibujarse.

**Tech Stack:** ASP.NET Core 8 (JwtBearer + cookies), xUnit, React 18, Vite 5, Vitest.

**Spec:** `docs/superpowers/specs/2026-10-07-sesion-cookies-design.md`

## Global Constraints

- Rama de trabajo: `feat/sesion-cookies` (sale de `feat/imagenes-habitaciones-impl`). No se hace `push`; lo hace el usuario.
- Commits con el correo `santirramos@gmail.com` (ya configurado) y **sin** `Co-Authored-By` ni menciones a Claude.
- Nunca imprimir ni commitear secretos ni tokens: `.env` (`SUPABASE_*`, `JWT_KEY`), el valor de la cookie `hotel_token`, ni el contenido de los archivos de cookies de `curl` (`-c`/`-b`). Para mostrar una cabecera `Set-Cookie`, enmascarar el valor (`sed -E 's/hotel_token=[^;]+/hotel_token=<oculto>/'`).
- Los `.env` son locales y están ignorados por git. Las únicas ediciones permitidas son líneas no secretas: `CORS_ORIGINS` en `auth-api/.env` (Task 3) y `VITE_AUTH_API_URL` en los `.env` de los fronts de módulo (Task 5); se hacen con un reemplazo puntual que no imprime el resto del archivo.
- Cookie de sesión: nombre `hotel_token`; `HttpOnly`; `SameSite=Lax`; `Path=/`; `Secure` solo si la petición es HTTPS; `Expires` = `exp` del token (120 minutos); sin `Domain`.
- Token (JWT): claims `sub`, `role`, `cliente_id` (solo huéspedes), `iss`, `aud`, `exp`. **Sin** `name` ni `email`.
- Login y registro responden **204 sin cuerpo**; los errores siguen siendo `{ "message": "..." }`. `GET /api/auth/me` devuelve exactamente `{ nombre, perfil }`.
- CSRF: toda petición POST, PUT, PATCH o DELETE debe traer `X-Requested-With: hotel-ui`, salvo las que traen `Authorization: Bearer`. Si falta: 400 `{ "message": "Falta la cabecera de seguridad." }`. GET, HEAD, OPTIONS y TRACE no la necesitan.
- Las APIs siguen aceptando `Authorization: Bearer` (Swagger, Postman). Si hay `Authorization`, se usa esa y se ignora la cookie.
- CORS: orígenes explícitos (nunca comodín) con `AllowCredentials()`.
- Textos de interfaz y mensajes en español. .NET 8: antes de usar `dotnet` en cada terminal, `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH`.
- Cada llamada de shell empieza de cero: las variables y funciones no persisten entre llamadas.
- Las pruebas contra servicios reales no deben dejar datos: lo que se suba se borra y los archivos temporales de cookies se eliminan al terminar.
- Puertos: auth-api 5001, clientes-api 5002, habitaciones-api 5003, reservas-api 5004; fronts 5173 (auth), 5174 (habitaciones), 5175 (clientes), 5176 (reservas).

## Review Focus

1. Una petición que modifica datos autenticada solo por cookie y sin `X-Requested-With`: 400 en **todas** las APIs, incluidas la subida multipart de imágenes y el logout. Una con `Authorization: Bearer` y sin la cabecera sí pasa. (Tasks 1, 3 y 8)
2. Cookie vencida, manipulada o ausente: `/me` da 401, `initSession()` devuelve `false` y el front va al login sin bucle de redirecciones. (Tasks 4 y 6)
3. Usuario desactivado con una cookie todavía vigente: `/me` da 401 y el front cierra la sesión (las APIs de módulo solo validan el token, así que el token sigue valiendo hasta su vencimiento: límite conocido, no revocación). (Task 2)
4. Logout: borra la cookie con los mismos atributos (`Path`, `SameSite`) y responde 204 también si la sesión ya venció. (Tasks 1 y 2)
5. La cookie es `Secure` solo con HTTPS; el login y el registro no devuelven ningún dato del usuario; el token no contiene nombre ni correo. (Tasks 1 y 2)

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `hotel-security/` (nuevo, biblioteca .NET) | `SesionCookie`, `TokenDesdeCookie`, `CsrfMiddleware` compartidos por las 4 APIs |
| `hotel-security.Tests/` (nuevo) | Pruebas xUnit de la biblioteca |
| `auth-api/Controllers/AuthController.cs`, `Program.cs`, `AuthApi.csproj`, `.env.example` | Cookie en login/registro, `/me` mínimo, `/logout`, CORS con credenciales, CSRF |
| `auth-api.Tests/` (nuevo) | Pruebas xUnit del controlador |
| `clientes-api`, `habitaciones-api`, `reservas-api` (`Program.cs`, `*.csproj`) | Token desde cookie, CORS con credenciales, CSRF |
| `hotel-ui/src/http.js` (nuevo), `session.js` (reescrito), `index.js`, tests, `README.md` | Cliente HTTP con sesión por cookie y sesión en memoria |
| `clientes-front`, `habitaciones-front`, `reservas-front` (`auth.js`, `api.js`, `main.jsx`, `.env.example`) | Usan el cliente y `initSession()` asíncrono |
| `auth-front/src/*` | Contexto de sesión, login/registro/home sin `sessionStorage` |
| `README.md`, `*/.env.example` | Documentación y variables |

---

### Task 1: Biblioteca `hotel-security` (TDD)

**Files:**
- Create: `hotel-security/HotelSecurity.csproj`
- Create: `hotel-security/SesionCookie.cs`, `hotel-security/TokenDesdeCookie.cs`, `hotel-security/CsrfMiddleware.cs`
- Create: `hotel-security.Tests/HotelSecurity.Tests.csproj`, `hotel-security.Tests/SesionCookieTests.cs`, `hotel-security.Tests/TokenDesdeCookieTests.cs`, `hotel-security.Tests/CsrfMiddlewareTests.cs`

**Interfaces:**
- Produces (namespace `HotelSecurity`):
  - `SesionCookie.Nombre` (`"hotel_token"`), `SesionCookie.Poner(HttpResponse response, string token, DateTimeOffset expira)`, `SesionCookie.Borrar(HttpResponse response)`.
  - `TokenDesdeCookie.Leer(MessageReceivedContext contexto)` → `Task`, para `JwtBearerEvents.OnMessageReceived`.
  - `CsrfMiddleware` (constantes `Cabecera = "X-Requested-With"`, `Valor = "hotel-ui"`) y `app.UseCsrfHeader()`.
- Comando de pruebas: `dotnet test hotel-security.Tests`.

- [ ] **Step 1: Crear los proyectos**

`hotel-security/HotelSecurity.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
  </ItemGroup>
</Project>
```

`hotel-security.Tests/HotelSecurity.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../hotel-security/HotelSecurity.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Escribir las pruebas que fallan**

`hotel-security.Tests/SesionCookieTests.cs`:

```csharp
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
```

`hotel-security.Tests/TokenDesdeCookieTests.cs`:

```csharp
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
```

`hotel-security.Tests/CsrfMiddlewareTests.cs`:

```csharp
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
```

- [ ] **Step 3: Ejecutar y comprobar que falla**

Run: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH; dotnet test hotel-security.Tests 2>&1 | grep -E "error CS|Passed!|Failed!" | sed -E 's#/Users/[^ ]*/##' | cut -c1-150 | sort -u | head -4`
Expected: errores de compilación `CS0103`/`CS0246` por `SesionCookie`, `TokenDesdeCookie` y `CsrfMiddleware` inexistentes.

- [ ] **Step 4: Implementar la biblioteca**

`hotel-security/SesionCookie.cs`:

```csharp
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
```

`hotel-security/TokenDesdeCookie.cs`:

```csharp
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
```

`hotel-security/CsrfMiddleware.cs`:

```csharp
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
```

- [ ] **Step 5: Ejecutar y comprobar que pasa**

Run: `dotnet test hotel-security.Tests 2>&1 | grep -E "error|Passed!|Failed!|\[FAIL\]" | cut -c1-140`
Expected: `Passed!  - Failed: 0, Passed: 23` (4 de cookies, 4 de lectura del token y 15 de CSRF contando los casos de las teorías).

- [ ] **Step 6: Commit**

```bash
git add hotel-security hotel-security.Tests
git commit -m "feat(hotel-security): cookie de sesión, lectura del token desde la cookie y middleware anti-CSRF"
```

---

### Task 2: `auth-api` con cookie, `/me` mínimo y `/logout` (TDD)

**Files:**
- Create: `auth-api.Tests/AuthApi.Tests.csproj`, `auth-api.Tests/Ayuda.cs`, `auth-api.Tests/SesionTests.cs`
- Modify: `auth-api/AuthApi.csproj`, `auth-api/Controllers/AuthController.cs`, `auth-api/Program.cs`, `auth-api/.env.example`

**Interfaces:**
- Consumes: `SesionCookie`, `CsrfMiddleware` / `UseCsrfHeader`, `TokenDesdeCookie` (Task 1).
- Produces: `POST /api/auth/login` y `POST /api/auth/registro` (204 + cookie); `GET /api/auth/me` → `{ nombre, perfil }` o 401; `POST /api/auth/logout` (204). `AuthController.EmitirToken(Usuario)` es privado. Comando de pruebas: `dotnet test auth-api.Tests`.

- [ ] **Step 1: Referenciar la biblioteca y crear el proyecto de pruebas**

En `auth-api/AuthApi.csproj`, agregar antes de `</Project>`:

```xml
  <ItemGroup>
    <ProjectReference Include="../hotel-security/HotelSecurity.csproj" />
  </ItemGroup>
```

`auth-api.Tests/AuthApi.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.4" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../auth-api/AuthApi.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Ayudas de prueba, `auth-api.Tests/Ayuda.cs`**

```csharp
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
```

- [ ] **Step 3: Escribir las pruebas que fallan, `auth-api.Tests/SesionTests.cs`**

```csharp
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
```

- [ ] **Step 4: Ejecutar y comprobar que falla**

Run: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH; dotnet test auth-api.Tests 2>&1 | grep -E "error CS|Passed!|Failed!" | sed -E 's#/Users/[^ ]*/##' | cut -c1-150 | sort -u | head -4`
Expected: error de compilación `CS1061` (`Logout` no existe) y/o fallos por el cuerpo y las cookies actuales.

- [ ] **Step 5: Implementar en `AuthController.cs`**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'auth-api/Controllers/AuthController.cs'
s = open(p, encoding='utf-8').read()

# using de la biblioteca
old = "using System.IdentityModel.Tokens.Jwt;\n"
assert s.startswith(old)
s = s.replace(old, old + "using HotelSecurity;\n", 1)

# Login y Registro: poner la cookie y no devolver datos
assert s.count("return Ok(Emitir(u));") == 2
s = s.replace("return Ok(Emitir(u));", "return IniciarSesion(u);")

# Reemplazar Emitir y Me por el nuevo bloque (hasta el final del archivo)
i = s.index("    private object Emitir(Usuario u)")
nuevo = """    // Pone la cookie de sesión y no devuelve ningún dato del usuario
    private IActionResult IniciarSesion(Usuario u)
    {
        var (token, expira) = EmitirToken(u);
        SesionCookie.Poner(Response, token, expira);
        return NoContent();
    }

    // El token solo lleva lo que las APIs necesitan: quién es (sub), su perfil (role) y, si es huésped, su ficha (cliente_id)
    private (string Token, DateTime Expira) EmitirToken(Usuario u)
    {
        var jwt = cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var expira = DateTime.UtcNow.AddMinutes(jwt.GetValue("ExpiraMinutos", 120));
        var claims = new List<Claim> { new("sub", u.Id.ToString()), new("role", u.Perfil) };
        if (u.ClienteId is not null) claims.Add(new Claim("cliente_id", u.ClienteId.ToString()!));
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims,
            expires: expira, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    // Quién soy: solo lo necesario para la interfaz. Se lee de la base de datos, no del token.
    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var id)) return Unauthorized();
        var u = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Activo);
        return u is null ? Unauthorized() : Ok(new { nombre = u.Nombre, perfil = u.Perfil });
    }

    // Una cookie HttpOnly solo la puede borrar el servidor
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        SesionCookie.Borrar(Response);
        return NoContent();
    }
}
"""
s = s[:i] + nuevo
open(p, 'w', encoding='utf-8').write(s)
print('AuthController actualizado')
EOF
```
Expected: `AuthController actualizado`.

- [ ] **Step 6: `Program.cs` de `auth-api`: token desde cookie, CORS con credenciales y CSRF**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'auth-api/Program.cs'
s = open(p, encoding='utf-8').read()

old = "using System.Text;\n"
assert s.startswith(old)
s = s.replace(old, old + "using HotelSecurity;\n", 1)

old = "    o.MapInboundClaims = false;\n"
assert s.count(old) == 1
s = s.replace(old, old + "    o.Events = new JwtBearerEvents { OnMessageReceived = TokenDesdeCookie.Leer };\n")

old = ".WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()"
assert s.count(old) == 1
s = s.replace(old, old + ".AllowCredentials()")

old = "app.UseAuthentication(); app.UseAuthorization();"
assert s.count(old) == 1
s = s.replace(old, "app.UseCsrfHeader();\n" + old)
open(p, 'w', encoding='utf-8').write(s)

p = 'auth-api/.env.example'
s = open(p, encoding='utf-8').read()
old = "CORS_ORIGINS=http://localhost:5173"
assert s.count(old) == 1
s = s.replace(old, "# Los 4 fronts (login, habitaciones, clientes, reservas): la sesión por cookie exige orígenes explícitos\nCORS_ORIGINS=http://localhost:5173,http://localhost:5174,http://localhost:5175,http://localhost:5176")
open(p, 'w', encoding='utf-8').write(s)
print('Program.cs y .env.example actualizados')
EOF
```
Expected: `Program.cs y .env.example actualizados`.

- [ ] **Step 7: Ejecutar y comprobar que pasa**

Run: `dotnet build auth-api 2>&1 | grep -E " error |Error\(s\)"; dotnet test auth-api.Tests 2>&1 | grep -E "error|Passed!|Failed!|\[FAIL\]" | cut -c1-150`
Expected: `0 Error(s)` y `Failed: 0` (12 pruebas). Si `Me_*` falla por el tipo de resultado (`UnauthorizedResult` frente a `UnauthorizedObjectResult`), ajustar la **prueba** al tipo que devuelve el controlador (`Unauthorized()` sin argumentos es `UnauthorizedResult`).

- [ ] **Step 8: Commit**

```bash
git add auth-api auth-api.Tests
git commit -m "feat(auth-api): sesión por cookie HttpOnly, /me mínimo, /logout y token sin nombre ni correo"
```

---

### Task 3: APIs de clientes, habitaciones y reservas leen la cookie

**Files:**
- Modify: `clientes-api/ClientesApi.csproj`, `habitaciones-api/HabitacionesApi.csproj`, `reservas-api/ReservasApi.csproj`
- Modify: `clientes-api/Program.cs`, `habitaciones-api/Program.cs`, `reservas-api/Program.cs`

**Interfaces:**
- Consumes: `TokenDesdeCookie.Leer`, `UseCsrfHeader()` (Task 1).
- Produces: las 3 APIs autentican con la cookie `hotel_token` (o con `Authorization`), aceptan credenciales en CORS y exigen `X-Requested-With: hotel-ui` en POST/PUT/PATCH/DELETE (salvo con Bearer).

- [ ] **Step 1: Referencia a la biblioteca y cambios de `Program.cs` en las 3 APIs**

Ejecutar:
```bash
python3 - <<'EOF'
for api in ('clientes-api', 'habitaciones-api', 'reservas-api'):
    # csproj
    import glob
    proj = glob.glob(f'{api}/*.csproj')[0]
    s = open(proj, encoding='utf-8').read()
    assert '</Project>' in s and 'hotel-security' not in s
    s = s.replace('</Project>', '  <ItemGroup>\n    <ProjectReference Include="../hotel-security/HotelSecurity.csproj" />\n  </ItemGroup>\n</Project>')
    open(proj, 'w', encoding='utf-8').write(s)

    # Program.cs
    p = f'{api}/Program.cs'
    s = open(p, encoding='utf-8').read()
    primera, resto = s.split('\n', 1)
    s = primera + '\nusing HotelSecurity;\n' + resto

    old = "    o.MapInboundClaims = false;\n"
    assert s.count(old) == 1, api
    s = s.replace(old, old + "    o.Events = new JwtBearerEvents { OnMessageReceived = TokenDesdeCookie.Leer };\n")

    old = ".AllowAnyMethod()"
    assert s.count(old) == 1, api
    s = s.replace(old, old + ".AllowCredentials()")

    old = "app.UseAuthentication(); app.UseAuthorization();"
    assert s.count(old) == 1, api
    s = s.replace(old, "app.UseCsrfHeader();\n" + old)
    open(p, 'w', encoding='utf-8').write(s)
    print(api, 'actualizada')
EOF
```
Expected: las 3 líneas `... actualizada`.

- [ ] **Step 2: Compilar y ejecutar las pruebas existentes**

Run:
```bash
export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH
for a in clientes-api habitaciones-api reservas-api; do printf "%-18s" $a; dotnet build $a 2>&1 | grep -E "Error\(s\)"; done
dotnet test habitaciones-api.Tests 2>&1 | tail -1 | cut -c1-110
```
Expected: `0 Error(s)` en las 3 y `Failed: 0` en las pruebas de habitaciones (40).

- [ ] **Step 3: Configuración local de `auth-api/.env` (no se commitea)**

La sesión por cookie exige que `auth-api` acepte a los 4 fronts. Reemplazar solo la línea `CORS_ORIGINS` del `.env` local (sin imprimir el archivo):
```bash
python3 - <<'EOF'
import re
p = 'auth-api/.env'
s = open(p, encoding='utf-8').read()
nueva = "CORS_ORIGINS=http://localhost:5173,http://localhost:5174,http://localhost:5175,http://localhost:5176"
if re.search(r'(?m)^CORS_ORIGINS=.*$', s):
    s = re.sub(r'(?m)^CORS_ORIGINS=.*$', nueva, s)
else:
    s = s.rstrip('\n') + '\n' + nueva + '\n'
open(p, 'w', encoding='utf-8').write(s)
print('CORS_ORIGINS de auth-api/.env actualizado (solo esa línea)')
EOF
git status --short
```
Expected: el mensaje y `git status` sin cambios en `.env` (está ignorado).

- [ ] **Step 4: Commit**

```bash
git add clientes-api habitaciones-api reservas-api
git commit -m "feat(apis): clientes, habitaciones y reservas aceptan la sesión por cookie con protección CSRF"
```

---

### Task 4: `hotel-ui`: cliente HTTP y sesión en memoria (TDD)

**Files:**
- Create: `hotel-ui/src/http.js`, `hotel-ui/src/http.test.js`
- Modify (reescritura): `hotel-ui/src/session.js`, `hotel-ui/src/session.test.js`
- Modify: `hotel-ui/src/index.js`, `hotel-ui/src/index.test.js`, `hotel-ui/README.md`

**Interfaces:**
- Produces:
  - `CABECERA_CSRF` (`{ nombre: 'X-Requested-With', valor: 'hotel-ui' }`).
  - `crearCliente({ base, onNoAutorizado? })` → `{ req(ruta, opciones?), fetchConSesion(ruta, opciones?) }`. `req` devuelve el JSON (o `null` en 204), lanza `Error(mensaje)` en respuestas no OK, y si hay 401 y `onNoAutorizado`, lo llama y devuelve `null`. Sin `onNoAutorizado`, un 401 lanza el mensaje del servidor (login).
  - `createSession({ authApiUrl, loginUrl, irA? })` → `{ initSession(): Promise<boolean>, usuarioActual(): {nombre, perfil}|null, tienePerfil(...perfiles), logout(): Promise<void> }`. **Cambia la firma anterior** (`createSession(loginUrl)`).

- [ ] **Step 1: Escribir las pruebas que fallan**

`hotel-ui/src/http.test.js`:

```js
import { crearCliente, CABECERA_CSRF } from './http.js'

const respuesta = (estado, cuerpo) => ({ ok: estado >= 200 && estado < 300, status: estado, json: async () => cuerpo })
let fetchFalso

beforeEach(() => { fetchFalso = vi.fn(); vi.stubGlobal('fetch', fetchFalso) })
afterEach(() => vi.unstubAllGlobals())

const ultimaLlamada = () => fetchFalso.mock.calls.at(-1)

describe('crearCliente', () => {
  test('manda las credenciales y no pone la cabecera anti-CSRF en las lecturas', async () => {
    fetchFalso.mockResolvedValue(respuesta(200, { ok: 1 }))
    const { req } = crearCliente({ base: 'http://api' })
    expect(await req('/api/x')).toEqual({ ok: 1 })
    const [url, opciones] = ultimaLlamada()
    expect(url).toBe('http://api/api/x')
    expect(opciones.credentials).toBe('include')
    expect(opciones.headers[CABECERA_CSRF.nombre]).toBeUndefined()
  })

  test.each(['POST', 'PUT', 'PATCH', 'DELETE'])('%s lleva la cabecera anti-CSRF', async (metodo) => {
    fetchFalso.mockResolvedValue(respuesta(204))
    const { req } = crearCliente({ base: 'http://api' })
    await req('/api/x', { method: metodo })
    expect(ultimaLlamada()[1].headers[CABECERA_CSRF.nombre]).toBe(CABECERA_CSRF.valor)
  })

  test('JSON lleva Content-Type y FormData no (lo pone el navegador)', async () => {
    fetchFalso.mockResolvedValue(respuesta(204))
    const { req } = crearCliente({ base: 'http://api' })
    await req('/api/x', { method: 'POST', body: '{}' })
    expect(ultimaLlamada()[1].headers['Content-Type']).toBe('application/json')
    await req('/api/x', { method: 'POST', body: new FormData() })
    expect(ultimaLlamada()[1].headers['Content-Type']).toBeUndefined()
  })

  // Review Focus 2
  test('un 401 con onNoAutorizado cierra la sesión y no lanza', async () => {
    fetchFalso.mockResolvedValue(respuesta(401, { message: 'x' }))
    const onNoAutorizado = vi.fn()
    const { req } = crearCliente({ base: 'http://api', onNoAutorizado })
    expect(await req('/api/x')).toBeNull()
    expect(onNoAutorizado).toHaveBeenCalledTimes(1)
  })

  test('un 401 sin onNoAutorizado lanza el mensaje del servidor (login)', async () => {
    fetchFalso.mockResolvedValue(respuesta(401, { message: 'Correo o contraseña incorrectos.' }))
    const { req } = crearCliente({ base: 'http://api' })
    await expect(req('/api/auth/login', { method: 'POST', body: '{}' })).rejects.toThrow('Correo o contraseña incorrectos.')
  })

  test('una respuesta de error lanza su mensaje', async () => {
    fetchFalso.mockResolvedValue(respuesta(409, { message: 'Ya existe' }))
    const { req } = crearCliente({ base: 'http://api' })
    await expect(req('/api/x', { method: 'POST', body: '{}' })).rejects.toThrow('Ya existe')
  })

  test('un 204 devuelve null', async () => {
    fetchFalso.mockResolvedValue(respuesta(204))
    const { req } = crearCliente({ base: 'http://api' })
    expect(await req('/api/x', { method: 'DELETE' })).toBeNull()
  })

  test('fetchConSesion también manda credenciales (descarga de reportes)', async () => {
    fetchFalso.mockResolvedValue(respuesta(200))
    const { fetchConSesion } = crearCliente({ base: 'http://api' })
    await fetchConSesion('/api/reportes/clientes/csv')
    expect(ultimaLlamada()[1].credentials).toBe('include')
  })

  test('un fallo de red da un mensaje claro', async () => {
    fetchFalso.mockRejectedValue(new TypeError('fail'))
    const { req } = crearCliente({ base: 'http://api' })
    await expect(req('/api/x')).rejects.toThrow('No se pudo conectar con el servidor')
  })

  test('no guarda nada en sessionStorage ni en localStorage', async () => {
    fetchFalso.mockResolvedValue(respuesta(200, {}))
    const { req } = crearCliente({ base: 'http://api' })
    await req('/api/x')
    expect(sessionStorage.length).toBe(0)
    expect(localStorage.length).toBe(0)
  })
})
```

`hotel-ui/src/session.test.js` (reemplaza el archivo completo):

```js
import { createSession } from './session.js'
import { CABECERA_CSRF } from './http.js'

const respuesta = (estado, cuerpo) => ({ ok: estado >= 200 && estado < 300, status: estado, json: async () => cuerpo })
let fetchFalso
let irA
let sesion

beforeEach(() => {
  fetchFalso = vi.fn()
  vi.stubGlobal('fetch', fetchFalso)
  irA = vi.fn()
  sesion = createSession({ authApiUrl: 'http://auth', loginUrl: 'http://login', irA })
})
afterEach(() => vi.unstubAllGlobals())

describe('createSession', () => {
  test('initSession pregunta a /me con credenciales y guarda nombre y perfil en memoria', async () => {
    fetchFalso.mockResolvedValue(respuesta(200, { nombre: 'Ana', perfil: 'admin', correo: 'no-debe-guardarse' }))
    expect(await sesion.initSession()).toBe(true)
    const [url, opciones] = fetchFalso.mock.calls[0]
    expect(url).toBe('http://auth/api/auth/me')
    expect(opciones.credentials).toBe('include')
    expect(sesion.usuarioActual()).toEqual({ nombre: 'Ana', perfil: 'admin' })
  })

  // Review Focus 2
  test('un 401 significa sin sesión', async () => {
    fetchFalso.mockResolvedValue(respuesta(401))
    expect(await sesion.initSession()).toBe(false)
    expect(sesion.usuarioActual()).toBeNull()
  })

  test('un fallo de red significa sin sesión (no lanza)', async () => {
    fetchFalso.mockRejectedValue(new TypeError('fail'))
    expect(await sesion.initSession()).toBe(false)
    expect(sesion.usuarioActual()).toBeNull()
  })

  test('tienePerfil usa el perfil de la memoria', async () => {
    fetchFalso.mockResolvedValue(respuesta(200, { nombre: 'Ana', perfil: 'recepcion' }))
    await sesion.initSession()
    expect(sesion.tienePerfil('admin', 'recepcion')).toBe(true)
    expect(sesion.tienePerfil('admin')).toBe(false)
  })

  test('antes de iniciar no hay usuario ni perfil', () => {
    expect(sesion.usuarioActual()).toBeNull()
    expect(sesion.tienePerfil('admin')).toBe(false)
  })

  test('logout llama a /logout con credenciales y cabecera, borra la memoria y va al login', async () => {
    fetchFalso.mockResolvedValueOnce(respuesta(200, { nombre: 'Ana', perfil: 'admin' }))
    await sesion.initSession()
    fetchFalso.mockResolvedValueOnce(respuesta(204))
    await sesion.logout()
    const [url, opciones] = fetchFalso.mock.calls[1]
    expect(url).toBe('http://auth/api/auth/logout')
    expect(opciones.method).toBe('POST')
    expect(opciones.credentials).toBe('include')
    expect(opciones.headers[CABECERA_CSRF.nombre]).toBe(CABECERA_CSRF.valor)
    expect(sesion.usuarioActual()).toBeNull()
    expect(irA).toHaveBeenCalledWith('http://login/login')
  })

  test('logout va al login aunque el servidor no responda', async () => {
    fetchFalso.mockRejectedValue(new TypeError('fail'))
    await sesion.logout()
    expect(irA).toHaveBeenCalledWith('http://login/login')
  })

  test('nunca guarda nada en sessionStorage ni en localStorage', async () => {
    fetchFalso.mockResolvedValue(respuesta(200, { nombre: 'Ana', perfil: 'admin' }))
    await sesion.initSession()
    await sesion.logout()
    expect(sessionStorage.length).toBe(0)
    expect(localStorage.length).toBe(0)
  })
})
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `cd hotel-ui && npx vitest run src/http.test.js src/session.test.js 2>&1 | grep -E "Failed to resolve|FAIL|Test Files|Tests " | head -6; cd ..`
Expected: `http.test.js` falla por `Failed to resolve import "./http.js"`; `session.test.js` falla (la firma y el comportamiento actuales son otros).

- [ ] **Step 3: Implementar `hotel-ui/src/http.js`**

```js
import { extraerError, fetchSeguro } from './validators.js'

// Como la sesión viaja en una cookie, las peticiones que modifican datos llevan esta cabecera (las APIs la exigen)
export const CABECERA_CSRF = { nombre: 'X-Requested-With', valor: 'hotel-ui' }

const LECTURAS = ['GET', 'HEAD', 'OPTIONS']
const modifica = (metodo = 'GET') => !LECTURAS.includes(metodo.toUpperCase())

// Cliente HTTP para una API: manda la cookie (credentials) y la cabecera anti-CSRF al modificar datos.
// `req` devuelve el JSON (null en 204) y lanza Error(mensaje) si la respuesta no es correcta.
// Con `onNoAutorizado`, un 401 lo llama (cerrar sesión) y devuelve null; sin él, el 401 se trata como cualquier error (login).
export function crearCliente({ base, onNoAutorizado }) {
  const fetchConSesion = (ruta, opciones = {}) => {
    const headers = {
      ...(modifica(opciones.method) ? { [CABECERA_CSRF.nombre]: CABECERA_CSRF.valor } : {}),
      ...opciones.headers,
    }
    return fetchSeguro(`${base}${ruta}`, { ...opciones, credentials: 'include', headers })
  }

  async function req(ruta, opciones = {}) {
    // Con FormData el navegador pone el Content-Type (multipart + boundary)
    const headers = { ...(opciones.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }), ...opciones.headers }
    const res = await fetchConSesion(ruta, { ...opciones, headers })
    if (res.status === 401 && onNoAutorizado) { onNoAutorizado(); return null }
    if (!res.ok) throw new Error(await extraerError(res))
    return res.status === 204 ? null : res.json()
  }

  return { req, fetchConSesion }
}
```

- [ ] **Step 4: Reescribir `hotel-ui/src/session.js`**

```js
import { CABECERA_CSRF } from './http.js'

// Sesión de los fronts de módulo. El token vive en una cookie HttpOnly que el JavaScript no puede leer:
// aquí solo se guarda, en memoria, { nombre, perfil } que responde auth-api en /me.
// `irA` existe para poder probar la redirección (por defecto cambia la URL del navegador).
export function createSession({ authApiUrl, loginUrl, irA = (url) => { window.location.href = url } }) {
  let usuario = null

  const initSession = async () => {
    try {
      const res = await fetch(`${authApiUrl}/api/auth/me`, { credentials: 'include' })
      if (!res.ok) { usuario = null; return false }
      const datos = await res.json()
      usuario = { nombre: datos.nombre, perfil: datos.perfil }
      return true
    } catch {
      usuario = null
      return false
    }
  }

  const usuarioActual = () => usuario
  const tienePerfil = (...perfiles) => perfiles.includes(usuario?.perfil)

  const logout = async () => {
    usuario = null
    try {
      await fetch(`${authApiUrl}/api/auth/logout`, {
        method: 'POST', credentials: 'include', headers: { [CABECERA_CSRF.nombre]: CABECERA_CSRF.valor },
      })
    } catch { /* sin conexión: igual se vuelve al login */ }
    irA(`${loginUrl}/login`)
  }

  return { initSession, usuarioActual, tienePerfil, logout }
}
```

- [ ] **Step 5: Exportar y probar el export**

En `hotel-ui/src/index.js`, agregar al final:

```js
export { crearCliente, CABECERA_CSRF } from './http.js'
```

En `hotel-ui/src/index.test.js`, agregar `'crearCliente', 'CABECERA_CSRF',` a la lista `PUBLICOS` (antes del `]` de cierre).

- [ ] **Step 6: Ejecutar toda la suite**

Run: `cd hotel-ui && npm test 2>&1 | grep -E "Test Files|Tests |×"; cd ..`
Expected: todo pasa (79 pruebas: las 64 de antes, menos 6 de la sesión anterior, más 8 de la sesión nueva y 13 de `http.test.js`).

- [ ] **Step 7: Actualizar el `README.md` de `hotel-ui`**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'hotel-ui/README.md'
s = open(p, encoding='utf-8').read()
i = s.index("## Sesión: `createSession(loginUrl)`")
j = s.index("## Validadores")
nuevo = """## Sesión: `createSession({ authApiUrl, loginUrl })`
La sesión vive en una cookie `HttpOnly` que emite `auth-api` y que el JavaScript no puede leer. Los módulos solo conocen, en memoria,
`{ nombre, perfil }` que responde `GET /api/auth/me`. Devuelve `{ initSession, usuarioActual, tienePerfil, logout }`.
- `await initSession()`: pregunta a `/me` con la cookie; guarda `{ nombre, perfil }` en memoria y devuelve `true`, o `false` si no hay sesión.
  Se espera antes de dibujar la aplicación (`main.jsx`).
- `usuarioActual()`: `{ nombre, perfil }` o `null`.
- `tienePerfil(...perfiles)`: `true` si el perfil actual está entre ellos.
- `await logout()`: llama a `POST /api/auth/logout` (el servidor borra la cookie) y redirige a `{loginUrl}/login`.
- Opcional `irA(url)`: cómo redirigir (por defecto cambia `window.location`); sirve para probar.

## Cliente HTTP: `crearCliente({ base, onNoAutorizado })`
Devuelve `{ req, fetchConSesion }` para hablar con una API usando la cookie de sesión.
- Manda siempre `credentials: 'include'` y, en POST, PUT, PATCH y DELETE, la cabecera `X-Requested-With: hotel-ui` (`CABECERA_CSRF`) que las APIs exigen.
- `req(ruta, opciones)`: devuelve el JSON (`null` en 204) y lanza `Error(mensaje)` si la respuesta no es correcta. Con `FormData` no fija el `Content-Type`.
- Un 401 llama a `onNoAutorizado` (normalmente `logout`) y devuelve `null`; sin `onNoAutorizado` se trata como cualquier error (útil en el login).
- `fetchConSesion(ruta, opciones)`: la petición cruda con credenciales (descarga de reportes).

```js
const { req } = crearCliente({ base: import.meta.env.VITE_API_URL, onNoAutorizado: logout })
export const clientesApi = { listar: () => req('/api/clientes'), crear: (d) => req('/api/clientes', { method: 'POST', body: JSON.stringify(d) }) }
```

"""
s = s[:i] + nuevo + s[j:]
open(p, 'w', encoding='utf-8').write(s)
print('README actualizado')
EOF
for n in crearCliente createSession CABECERA_CSRF fetchConSesion; do grep -q "$n" hotel-ui/README.md || echo "FALTA: $n"; done; echo revisado
```
Expected: `README actualizado` y `revisado` sin ninguna línea `FALTA:`.

- [ ] **Step 8: Commit**

```bash
git add hotel-ui
git commit -m "feat(hotel-ui): sesión en memoria con cookie y cliente HTTP con credenciales y protección CSRF"
```

---

### Task 5: Fronts de módulo (clientes, habitaciones, reservas)

**Files:**
- Modify: `clientes-front/src/auth.js`, `clientes-front/src/api.js`, `clientes-front/src/main.jsx`
- Modify: `habitaciones-front/src/auth.js`, `habitaciones-front/src/api.js`, `habitaciones-front/src/main.jsx`
- Modify: `reservas-front/src/auth.js`, `reservas-front/src/api.js`, `reservas-front/src/main.jsx`
- Create: `clientes-front/.env.example`, `habitaciones-front/.env.example`, `reservas-front/.env.example`

**Interfaces:**
- Consumes: `createSession`, `crearCliente` (Task 4); `GET /api/auth/me` y `POST /api/auth/logout` de `auth-api`.
- Produces: en cada módulo, `auth.js` exporta `{ initSession, logout, usuarioActual, tienePerfil }` (ya no `getToken`); `api.js` usa `crearCliente`.

- [ ] **Step 1: `auth.js` de los 3 módulos (mismo contenido)**

```js
import { createSession } from 'hotel-ui'

export const { initSession, logout, usuarioActual, tienePerfil } = createSession({
  authApiUrl: import.meta.env.VITE_AUTH_API_URL,
  loginUrl: import.meta.env.VITE_AUTH_URL,
})
```

- [ ] **Step 2: `main.jsx` de los 3 módulos (mismo contenido)**

```jsx
import React from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.jsx'
import 'hotel-ui/theme.css'
import './styles.css'
import { initSession, logout } from './auth.js'

// Se pregunta a auth-api quién es el usuario antes de dibujar; sin sesión se vuelve al login
initSession().then((haySesion) => {
  if (!haySesion) return logout()
  createRoot(document.getElementById('root')).render(
    <BrowserRouter><App /></BrowserRouter>
  )
})
```

- [ ] **Step 3: `api.js` de `clientes-front`**

Reemplazar la cabecera y la función `req` del archivo (todo lo anterior a `export const clientesApi`) por:

```js
import { crearCliente } from 'hotel-ui'
import { logout } from './auth.js'

const { req, fetchConSesion } = crearCliente({ base: import.meta.env.VITE_API_URL, onNoAutorizado: logout })

```

y reemplazar la función `descargarReporte` por:

```js
export async function descargarReporte(tipo, { q = '', soloActivos = false } = {}) {
  const res = await fetchConSesion(`/api/reportes/clientes/${tipo}?q=${encodeURIComponent(q)}&soloActivos=${soloActivos}`)
  if (res.status === 401) return logout()
  if (!res.ok) throw new Error('No se pudo generar el reporte')
  const url = URL.createObjectURL(await res.blob())
  const a = document.createElement('a')
  a.href = url
  a.download = `clientes.${tipo}`
  a.click()
  URL.revokeObjectURL(url)
}
```

El objeto `clientesApi` no cambia.

- [ ] **Step 4: `api.js` de `habitaciones-front`**

Reemplazar todo lo anterior a `export const habitacionesApi` (los imports, `BASE`, `auth` y `req`) por:

```js
import { crearCliente } from 'hotel-ui'
import { logout } from './auth.js'

const { req } = crearCliente({ base: import.meta.env.VITE_API_URL, onNoAutorizado: logout })

```

El objeto `habitacionesApi`, `ESTADOS` y `TIPOS` no cambian (la subida de imágenes ya usa `FormData`, que `crearCliente` maneja).

- [ ] **Step 5: `api.js` de `reservas-front`**

Reemplazar todo lo anterior a `export const reservasApi` por:

```js
import { crearCliente } from 'hotel-ui'
import { logout } from './auth.js'

const { req } = crearCliente({ base: import.meta.env.VITE_API_URL, onNoAutorizado: logout })

```

`reservasApi`, `ESTADOS`, `cop` y `miApi` no cambian.

- [ ] **Step 6: `.env.example` de los 3 módulos y variable local**

Crear:

`clientes-front/.env.example`:
```
# URL de la API de este módulo
VITE_API_URL=http://localhost:5002
# Front de login (auth-front) y API de autenticación (auth-api): la sesión va en una cookie que emite auth-api
VITE_AUTH_URL=http://localhost:5173
VITE_AUTH_API_URL=http://localhost:5001
```

`habitaciones-front/.env.example`: igual pero `VITE_API_URL=http://localhost:5003`.
`reservas-front/.env.example`: igual pero `VITE_API_URL=http://localhost:5004`.

Agregar la variable nueva a los `.env` locales (no se commitean; solo esa línea, sin imprimir el archivo):
```bash
python3 - <<'EOF'
for d in ('clientes-front', 'habitaciones-front', 'reservas-front'):
    p = f'{d}/.env'
    s = open(p, encoding='utf-8').read()
    if 'VITE_AUTH_API_URL=' not in s:
        s = s.rstrip('\n') + '\nVITE_AUTH_API_URL=http://localhost:5001\n'
        open(p, 'w', encoding='utf-8').write(s)
    print(d, 'ok')
EOF
git status --short
```
Expected: 3 líneas `... ok` y `git status` mostrando solo los 3 `.env.example` nuevos y los archivos modificados.

- [ ] **Step 7: Compilar y comprobar que no queda sesión en `sessionStorage`**

Run:
```bash
for d in clientes-front habitaciones-front reservas-front; do printf "%-20s" $d; (cd $d && npx vite build 2>&1 | tail -1); done
grep -rn "sessionStorage\|localStorage\|getToken\|Authorization\|#token" clientes-front/src habitaciones-front/src reservas-front/src hotel-ui/src --include='*.js' --include='*.jsx' | grep -v "\.test\."; echo "(fin)"
```
Expected: los 3 fronts terminan con `✓ built in ...` y el `grep` no imprime nada antes de `(fin)`.

- [ ] **Step 8: Commit**

```bash
git add clientes-front habitaciones-front reservas-front
git commit -m "feat(fronts): los módulos usan la sesión por cookie y el cliente HTTP de hotel-ui"
```

---

### Task 6: `auth-front`: contexto de sesión reactivo

**Files:**
- Create: `auth-front/src/SesionContext.jsx`, `auth-front/.env.example`
- Modify: `auth-front/src/api.js`, `auth-front/src/main.jsx`, `auth-front/src/App.jsx`
- Modify: `auth-front/src/pages/Login.jsx`, `auth-front/src/pages/Registro.jsx`, `auth-front/src/pages/Home.jsx`
- Delete: `auth-front/src/session.js`

**Interfaces:**
- Consumes: `crearCliente`, `Spinner`, `Card`, `AppShell`, `Alert`, `Button`, `Field`, `Input`, `Select` de `hotel-ui`; `POST /api/auth/login`, `POST /api/auth/registro`, `GET /api/auth/me`, `POST /api/auth/logout`.
- Produces: `useSesion()` → `{ cargando, usuario, entrar(email, password), registrar(datos), salir() }`; `rutaPorPerfil(perfil)`. `entrar` y `registrar` devuelven `{ nombre, perfil }` o `null` si no se pudo conocer al usuario.

- [ ] **Step 1: `auth-front/src/api.js` (reemplazo completo)**

```js
import { crearCliente } from 'hotel-ui'

// Sin onNoAutorizado: un 401 en el login es un error normal («Correo o contraseña incorrectos»)
const { req } = crearCliente({ base: import.meta.env.VITE_API_URL })

export const login = (email, password) => req('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
export const registro = (datos) => req('/api/auth/registro', { method: 'POST', body: JSON.stringify(datos) })
export const yo = () => req('/api/auth/me')
export const salirDelServidor = () => req('/api/auth/logout', { method: 'POST' })
```

- [ ] **Step 2: `auth-front/src/SesionContext.jsx`**

```jsx
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import * as api from './api.js'

const Contexto = createContext(null)
export const useSesion = () => useContext(Contexto)

// Cada perfil aterriza en su propia pantalla
const RUTAS = { admin: '/admin', recepcion: '/recepcion', huesped: '/huesped' }
export const rutaPorPerfil = (perfil) => RUTAS[perfil] ?? '/login'

// La sesión es una cookie HttpOnly: aquí solo se guarda, en memoria, quién es el usuario (nombre y perfil)
export function SesionProvider({ children }) {
  const [estado, setEstado] = useState({ cargando: true, usuario: null })

  const refrescar = useCallback(async () => {
    try {
      const datos = await api.yo()
      const usuario = datos ? { nombre: datos.nombre, perfil: datos.perfil } : null
      setEstado({ cargando: false, usuario })
      return usuario
    } catch {
      setEstado({ cargando: false, usuario: null })
      return null
    }
  }, [])

  useEffect(() => { refrescar() }, [refrescar])

  const entrar = useCallback(async (email, password) => { await api.login(email, password); return refrescar() }, [refrescar])
  const registrar = useCallback(async (datos) => { await api.registro(datos); return refrescar() }, [refrescar])
  const salir = useCallback(async () => {
    try { await api.salirDelServidor() } catch { /* sin conexión: igual se cierra la sesión en pantalla */ }
    setEstado({ cargando: false, usuario: null })
  }, [])

  const valor = useMemo(() => ({ ...estado, entrar, registrar, salir }), [estado, entrar, registrar, salir])
  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>
}
```

- [ ] **Step 3: `main.jsx` y `App.jsx`**

`auth-front/src/main.jsx`:
```jsx
import React from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.jsx'
import { SesionProvider } from './SesionContext.jsx'
import 'hotel-ui/theme.css'
import './styles.css'

createRoot(document.getElementById('root')).render(
  <BrowserRouter><SesionProvider><App /></SesionProvider></BrowserRouter>
)
```

`auth-front/src/App.jsx`:
```jsx
import { Navigate, Route, Routes } from 'react-router-dom'
import { Spinner } from 'hotel-ui'
import Login from './pages/Login.jsx'
import Home from './pages/Home.jsx'
import Registro from './pages/Registro.jsx'
import { rutaPorPerfil, useSesion } from './SesionContext.jsx'

// Protección de rutas: exige sesión y, si se indica, un perfil concreto. Se evalúa en cada render (la sesión es reactiva).
function Protegida({ perfil, children }) {
  const { usuario } = useSesion()
  if (!usuario) return <Navigate to="/login" replace />
  if (perfil && usuario.perfil !== perfil) return <Navigate to={rutaPorPerfil(usuario.perfil)} replace />
  return children
}

// Login y registro: si ya hay sesión, se va a la pantalla del perfil
function Publica({ children }) {
  const { usuario } = useSesion()
  return usuario ? <Navigate to={rutaPorPerfil(usuario.perfil)} replace /> : children
}

export default function App() {
  const { cargando } = useSesion()
  if (cargando) return <div className="login-wrap"><Spinner /></div>
  return (
    <Routes>
      <Route path="/login" element={<Publica><Login /></Publica>} />
      <Route path="/registro" element={<Publica><Registro /></Publica>} />
      <Route path="/admin" element={<Protegida perfil="admin"><Home /></Protegida>} />
      <Route path="/recepcion" element={<Protegida perfil="recepcion"><Home /></Protegida>} />
      <Route path="/huesped" element={<Protegida perfil="huesped"><Home /></Protegida>} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
```

- [ ] **Step 4: `Login.jsx`, `Registro.jsx`, `Home.jsx`**

`auth-front/src/pages/Login.jsx` (reemplazo completo):

```jsx
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Alert, Button, Card, Field, Input } from 'hotel-ui'
import { rutaPorPerfil, useSesion } from '../SesionContext.jsx'

export default function Login() {
  const nav = useNavigate()
  const { entrar } = useSesion()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const enviar = async (e) => {
    e.preventDefault()
    setLoading(true); setError('')
    try {
      const usuario = await entrar(email.trim(), password)
      if (!usuario) throw new Error('No se pudo iniciar la sesión. Revisa que el navegador permita las cookies.')
      nav(rutaPorPerfil(usuario.perfil), { replace: true })
    } catch (err) { setError(err.message) } finally { setLoading(false) }
  }

  return (
    <div className="login-wrap">
      <Card as="form" className="login" onSubmit={enviar}>
        <h1>Hotel</h1>
        <p className="sub">Ingresa con tu cuenta</p>
        <Alert>{error}</Alert>
        <Field label="Correo"><Input type="email" required autoFocus value={email} onChange={(e) => setEmail(e.target.value)} /></Field>
        <Field label="Contraseña"><Input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} /></Field>
        <Button disabled={loading}>{loading ? 'Ingresando…' : 'Ingresar'}</Button>
        <p className="sub"><Link to="/registro">¿Eres huésped? Crea tu cuenta</Link></p>
      </Card>
    </div>
  )
}
```

`auth-front/src/pages/Registro.jsx` (reemplazo completo):

```jsx
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Alert, Button, Card, Field, Input, Select,
  validar, requerido, documento, letras, email, telefono, password, maxLen,
} from 'hotel-ui'
import { rutaPorPerfil, useSesion } from '../SesionContext.jsx'

const vacio = { tipoDocumento: 'CC', numeroDocumento: '', nombres: '', apellidos: '', email: '', telefono: '', password: '', confirmar: '' }
const ESQUEMA = {
  numeroDocumento: [requerido(), documento()],
  nombres: [requerido(), letras, maxLen(80)],
  apellidos: [requerido(), letras, maxLen(80)],
  email: [requerido(), email, maxLen(120)],
  telefono: [telefono],
  password: [requerido(), password],
  confirmar: [requerido('Confirma tu contraseña'), (v, f) => (v !== f.password ? 'Las contraseñas no coinciden' : null)],
}

export default function Registro() {
  const nav = useNavigate()
  const { registrar } = useSesion()
  const [f, setF] = useState(vacio)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [intento, setIntento] = useState(false)
  const errs = intento ? validar(f, ESQUEMA) : {}
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })

  const enviar = async (e) => {
    e.preventDefault()
    setIntento(true)
    if (Object.keys(validar(f, ESQUEMA)).length) return
    setLoading(true); setError('')
    try {
      const { confirmar, ...datos } = f
      const usuario = await registrar({ ...datos, email: f.email.trim(), telefono: f.telefono || null })
      if (!usuario) throw new Error('Tu cuenta se creó, pero no se pudo iniciar la sesión. Inicia sesión manualmente.')
      nav(rutaPorPerfil(usuario.perfil), { replace: true })
    } catch (err) { setError(err.message) } finally { setLoading(false) }
  }

  return (
    <div className="login-wrap">
      <Card as="form" className="login" onSubmit={enviar} noValidate>
        <h1>Crear cuenta</h1>
        <p className="sub">Regístrate para reservar tu estadía</p>
        <Alert>{error}</Alert>
        <Field label="Tipo de documento">
          <Select value={f.tipoDocumento} onChange={set('tipoDocumento')} options={['CC', 'CE', 'TI', 'PA']} />
        </Field>
        <Field label="Número de documento" error={errs.numeroDocumento}><Input value={f.numeroDocumento} onChange={set('numeroDocumento')} /></Field>
        <Field label="Nombres" error={errs.nombres}><Input value={f.nombres} onChange={set('nombres')} /></Field>
        <Field label="Apellidos" error={errs.apellidos}><Input value={f.apellidos} onChange={set('apellidos')} /></Field>
        <Field label="Correo" error={errs.email}><Input type="email" value={f.email} onChange={set('email')} /></Field>
        <Field label="Teléfono (opcional)" error={errs.telefono}><Input value={f.telefono} onChange={set('telefono')} /></Field>
        <Field label="Contraseña (8+ caracteres, mayúscula, minúscula y número)" error={errs.password}><Input type="password" value={f.password} onChange={set('password')} /></Field>
        <Field label="Confirmar contraseña" error={errs.confirmar}><Input type="password" value={f.confirmar} onChange={set('confirmar')} /></Field>
        <Button disabled={loading}>{loading ? 'Creando…' : 'Crear cuenta'}</Button>
        <p className="sub"><Link to="/login">Ya tengo cuenta</Link></p>
      </Card>
    </div>
  )
}
```

`auth-front/src/pages/Home.jsx` (reemplazo completo):

```jsx
import { useNavigate } from 'react-router-dom'
import { AppShell, Card } from 'hotel-ui'
import { useSesion } from '../SesionContext.jsx'

const CLIENTES = import.meta.env.VITE_CLIENTES_URL
const HABITACIONES = import.meta.env.VITE_HABITACIONES_URL
const RESERVAS = import.meta.env.VITE_RESERVAS_URL

const ETIQUETAS = { admin: 'Administrador', recepcion: 'Recepción', huesped: 'Huésped' }

// Módulos visibles por perfil
const MODULOS = [
  { nombre: 'Clientes', desc: 'Registro, consulta y reportes CSV/PDF', url: CLIENTES, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Habitaciones', desc: 'Disponibilidad y gestión de habitaciones', url: HABITACIONES, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Reservas', desc: 'Reservas, check-in y check-out', url: RESERVAS, perfiles: ['admin', 'recepcion'] },
  { nombre: 'Mis reservas', desc: 'Reserva tu estadía y consulta tus reservas', url: RESERVAS, perfiles: ['huesped'] },
  { nombre: 'Usuarios', desc: 'Administración de cuentas y perfiles', url: null, perfiles: ['admin'] },
]

export default function Home() {
  const nav = useNavigate()
  const { usuario, salir } = useSesion()
  const cerrarSesion = async () => { await salir(); nav('/login', { replace: true }) }
  // La sesión va en la cookie: no hace falta pasar nada por la URL
  const abrir = (url) => { window.location.href = url }

  return (
    <AppShell section="Inicio" user={`${usuario.nombre} · ${ETIQUETAS[usuario.perfil]}`} onLogout={cerrarSesion}>
      <h2>Bienvenido, {usuario.nombre}</h2>
      <div className="modules">
        {MODULOS.filter((m) => m.perfiles.includes(usuario.perfil)).map((m) => (
          <Card as="button" key={m.nombre} className="module" disabled={!m.url} onClick={() => abrir(m.url)}>
            <strong>{m.nombre}</strong><span>{m.desc}</span>
            {!m.url && <small>Próximamente</small>}
          </Card>
        ))}
      </div>
    </AppShell>
  )
}
```

- [ ] **Step 5: Eliminar `session.js`, crear `.env.example` y compilar**

Run:
```bash
git rm -q auth-front/src/session.js
cat > auth-front/.env.example <<'EOF'
# auth-api (login, registro, /me y /logout) y la URL de cada módulo
VITE_API_URL=http://localhost:5001
VITE_CLIENTES_URL=http://localhost:5175
VITE_HABITACIONES_URL=http://localhost:5174
VITE_RESERVAS_URL=http://localhost:5176
EOF
cd auth-front && npx vite build 2>&1 | tail -2; cd ..
grep -rn "sessionStorage\|localStorage\|session.js\|saveSession\|getSession\|clearSession\|#token" auth-front/src; echo "(fin)"
grep -rn "<button\|<input\|<select" auth-front/src; echo "(fin crudos)"
```
Expected: `✓ built in ...`; los dos `grep` no imprimen nada antes de sus `(fin)`.

- [ ] **Step 6: Commit**

```bash
git add auth-front
git commit -m "feat(auth-front): contexto de sesión reactivo con cookie; sin sessionStorage ni token en la URL"
```

---

### Task 7: Documentación

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Actualizar el README raíz**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'README.md'
s = open(p, encoding='utf-8').read()

# 1) Flujo de autenticación
i = s.index("## Flujo AUTH-BT (Bearer Token)")
j = s.index("## Habitaciones (Integrante 3)")
nuevo = """## Flujo de sesión (cookie HttpOnly)
Login → `auth-api` valida con BCrypt, firma un JWT (solo `sub`, `role` y, si es huésped, `cliente_id`) y lo pone en la cookie
`hotel_token` (`HttpOnly`, `SameSite=Lax`, `Secure` con HTTPS); responde **204 sin cuerpo**. El navegador no puede leer la cookie ni
guarda nada en `sessionStorage`. Los fronts preguntan quién es el usuario a `GET /api/auth/me`, que devuelve solo `{ nombre, perfil }`,
y lo guardan únicamente en memoria. Los módulos no necesitan que se les pase ningún token: la cookie viaja sola a cada API, que valida
firma, issuer y audience con la misma `JWT_KEY`. Sin sesión o con un 401, el front vuelve al login. Salir llama a `POST /api/auth/logout`.

- **CSRF:** las peticiones POST, PUT, PATCH y DELETE deben traer `X-Requested-With: hotel-ui` (lo añade `hotel-ui`); si falta, 400.
  Las peticiones con `Authorization: Bearer` (Swagger, Postman) no la necesitan, y las APIs siguen aceptándolas.
- **Mismo host:** la cookie se comparte entre puertos porque todo corre en `localhost`. En otro entorno, fronts y APIs deben compartir host
  o dominio.
- **CORS:** `CORS_ORIGINS` de `auth-api` debe listar los 4 fronts (ver `auth-api/.env.example`).
- **Cambio incompatible:** las sesiones abiertas antes de este cambio dejan de valer; hay que volver a iniciar sesión.

"""
s = s[:i] + nuevo + s[j:]

# 2) Variables de los fronts
old = "### 4. Fronts\n"
assert s.count(old) == 1
s = s.replace(old, old + "Antes, en cada front: `cp .env.example .env` (define las URLs de las APIs; `VITE_AUTH_API_URL` es la de `auth-api`).\n")
open(p, 'w', encoding='utf-8').write(s)
print('README actualizado')
EOF
grep -c "hotel_token" README.md
```
Expected: `README actualizado` y un número mayor que 0.

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: flujo de sesión por cookie, CSRF y variables de los fronts"
```

---

### Task 8: Verificación de extremo a extremo y limpieza

**Files:** ninguno (verificación). Los hallazgos que obliguen a cambiar código se corrigen en la tarea correspondiente con su prueba.

- [ ] **Step 1: Suites y compilaciones**

Run:
```bash
export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH
for t in hotel-security.Tests auth-api.Tests habitaciones-api.Tests; do printf "%-24s" $t; dotnet test $t 2>&1 | tail -1 | cut -c1-80; done
for a in clientes-api reservas-api; do printf "%-24s" $a; dotnet build $a 2>&1 | grep -E "Error\(s\)"; done
npm --prefix hotel-ui test 2>&1 | grep -E "Test Files|Tests "
for d in auth-front clientes-front habitaciones-front reservas-front; do printf "%-24s" $d; (cd $d && npx vite build 2>&1 | tail -1); done
git status --short
```
Expected: todas las suites con `Failed: 0`, las dos APIs sin errores, los 4 fronts compilan y el árbol está limpio.

- [ ] **Step 2: Levantar los servicios**

Run:
```bash
export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH
echo "puertos ocupados antes: $(for p in 5001 5002 5003 5004 5173 5174 5175 5176; do lsof -tiTCP:$p -sTCP:LISTEN; done | wc -l | tr -d ' ')"
p=5001; for d in auth-api clientes-api habitaciones-api reservas-api; do (cd $d && nohup dotnet run --urls http://localhost:$p > "$TMPDIR/$d.log" 2>&1 &); p=$((p+1)); done
for d in auth-front clientes-front habitaciones-front reservas-front; do (cd $d && nohup npm run dev > "$TMPDIR/$d.log" 2>&1 &); done
sleep 25; for p in 5001 5002 5003 5004 5173 5174 5175 5176; do echo "$p -> $(curl -s -o /dev/null -w '%{http_code}' --max-time 5 http://localhost:$p/)"; done
```
Expected: 0 puertos ocupados antes y los 8 responden (404 en las APIs, 200 en los fronts).

- [ ] **Step 3: Probar el API con cookie (curl, sin imprimir el token)**

Run (todo en una sola llamada de shell; el archivo de cookies queda en `$TMPDIR` y no se imprime):
```bash
J="$TMPDIR/hotel-cookies.txt"; H='X-Requested-With: hotel-ui'; A=http://localhost:5001; M=http://localhost:5003
echo "1) login SIN la cabecera anti-CSRF:"; curl -s -w " [%{http_code}]\n" -X POST $A/api/auth/login -H 'Content-Type: application/json' -d '{"email":"admin@hotel.com","password":"Admin123*"}'
echo "2) login con cabecera -> estado y Set-Cookie (valor oculto):"; curl -s -D - -o /dev/null -c "$J" -X POST $A/api/auth/login -H "$H" -H 'Content-Type: application/json' -d '{"email":"admin@hotel.com","password":"Admin123*"}' | grep -iE "^HTTP|^set-cookie|^content-length" | sed -E 's/hotel_token=[^;]+/hotel_token=<oculto>/'
echo "3) /me con la cookie:"; curl -s -b "$J" $A/api/auth/me; echo
echo "4) claims del token en la cookie (solo nombres):"; python3 - <<EOF
import base64, json
for l in open("$J"):
    if 'hotel_token' in l:
        t = l.split('\t')[-1].strip().split('.')[1]
        t += '=' * (-len(t) % 4)
        print(sorted(json.loads(base64.urlsafe_b64decode(t)).keys()))
EOF
echo "5) lectura en habitaciones-api (otro puerto) solo con la cookie: $(curl -s -o /dev/null -w '%{http_code}' -b "$J" "$M/api/habitaciones?q=301")"
echo "6) sin cookie: $(curl -s -o /dev/null -w '%{http_code}' "$M/api/habitaciones?q=301")"
echo "7) PUT estado SIN cabecera anti-CSRF (cookie):"; HID=$(curl -s -b "$J" "$M/api/habitaciones?q=301" | sed -E 's/.*"items":\[\{"id":"([^"]+)".*/\1/'); curl -s -w " [%{http_code}]\n" -X PATCH -b "$J" -H 'Content-Type: application/json' -d '{"estado":"Disponible"}' $M/api/habitaciones/$HID/estado
echo "8) subida multipart SIN cabecera:"; printf '\211PNG\r\n\032\n' > "$TMPDIR/p.png"; head -c 64 /dev/zero >> "$TMPDIR/p.png"; curl -s -w " [%{http_code}]\n" -b "$J" -F "archivo=@$TMPDIR/p.png;type=image/png" $M/api/habitaciones/$HID/imagenes
echo "9) subida multipart CON cabecera:"; R=$(curl -s -w "\n%{http_code}" -b "$J" -H "$H" -F "archivo=@$TMPDIR/p.png;type=image/png" $M/api/habitaciones/$HID/imagenes); echo "$R" | tail -1; IMG=$(echo "$R" | head -1 | sed -E 's/.*"id":"([^"]+)".*/\1/')
echo "10) borrar esa imagen (cabecera + cookie): $(curl -s -o /dev/null -w '%{http_code}' -X DELETE -b "$J" -H "$H" $M/api/habitaciones/$HID/imagenes/$IMG)"
echo "11) Bearer sin cabecera anti-CSRF (reordenar lista vacía):"; TOK=$(python3 -c "
for l in open('$J'):
    if 'hotel_token' in l: print(l.split('\t')[-1].strip())"); curl -s -o /dev/null -w "[%{http_code}]\n" -X PUT -H "Authorization: Bearer $TOK" -H 'Content-Type: application/json' -d '{"ids":[]}' $M/api/habitaciones/$HID/imagenes/orden
echo "12) logout SIN cabecera:"; curl -s -w " [%{http_code}]\n" -X POST -b "$J" $A/api/auth/logout
echo "13) logout con cabecera -> Set-Cookie:"; curl -s -D - -o /dev/null -b "$J" -c "$J" -X POST $A/api/auth/logout -H "$H" | grep -iE "^HTTP|^set-cookie" | sed -E 's/hotel_token=[^;]*/hotel_token=<borrada>/'
echo "14) /me tras el logout: $(curl -s -o /dev/null -w '%{http_code}' -b "$J" $A/api/auth/me)"
rm "${J:?}" "${TMPDIR:?}/p.png"; echo "archivos temporales borrados"
```
Expected: 1) `400` «Falta la cabecera de seguridad.»; 2) `204`, un `Set-Cookie` con `httponly`, `samesite=lax` y `path=/` (sin `secure`) y sin cuerpo; 3) `{"nombre":"Administrador","perfil":"admin"}`; 4) `['exp', 'iss', 'aud', 'role', 'sub']` (en cualquier orden, sin `name` ni `email`); 5) `200`; 6) `401`; 7) `400`; 8) `400`; 9) `201`; 10) `204`; 11) `200`; 12) `400`; 13) `204` con la cookie expirada; 14) `401`.

- [ ] **Step 4: Verificar en el navegador integrado**

1. Abrir `http://localhost:5173/login` y entrar con `admin@hotel.com` (contraseña de prueba del README).
2. Ejecutar en la consola de la página (herramienta de inspección): `({ cookies: document.cookie, sesion: sessionStorage.length, local: localStorage.length })`. Esperado: `cookies` sin `hotel_token` (es HttpOnly), `sesion: 0` y `local: 0`.
3. Pulsar un módulo desde el panel: la URL del módulo no lleva `#token` y se carga sin volver al login. Recorrer Clientes, Habitaciones y Reservas: se muestran con el nombre del usuario arriba.
4. En Habitaciones → Editar → Imágenes, comprobar que carga (la subida real se probó con `curl`).
5. Volver al panel y pulsar **Salir**: aparece el formulario de login (no una pantalla en blanco). Entrar otra vez **sin recargar la página** y llegar al panel.
6. Con la sesión cerrada, abrir directamente `http://localhost:5174/habitaciones`: redirige al login.
7. Revisar la consola: sin errores de red nuevos ni «Maximum update depth exceeded».

- [ ] **Step 5: Limpieza y cierre**

Run:
```bash
pkill -f "dotnet run --urls"; pkill -f vite
for p in 5001 5002 5003 5004 5173 5174 5175 5176; do lsof -tiTCP:$p -sTCP:LISTEN | xargs kill 2>/dev/null; done; sleep 3
echo "escuchando: $(for p in 5001 5002 5003 5004 5173 5174 5175 5176; do lsof -tiTCP:$p -sTCP:LISTEN; done | wc -l | tr -d ' ')"
git status --short
```
Expected: `escuchando: 0` y árbol limpio. Confirmar que la imagen de prueba se borró (la habitación 301 no debe tener imágenes). Después, descartar la tarea pendiente «Corregir bucle infinito al cerrar sesión en auth-front» (task_5214f60e) con `mcp__ccd_session__dismiss_task`, porque este cambio la resuelve (el Step 4.5 lo comprueba).

- [ ] **Step 6: Informar al usuario**

Resumen de lo verificado, los commits de `feat/sesion-cookies`, que no se hizo `push`, los dos cambios locales de `.env` (solo `CORS_ORIGINS` de `auth-api` y `VITE_AUTH_API_URL` en los 3 fronts de módulo) y que las sesiones abiertas dejan de valer.
