# Imágenes de habitaciones (carrusel) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que cada habitación tenga hasta 6 imágenes guardadas en Supabase Storage, que admin y recepción las gestionen en el módulo de habitaciones y que se vean como carrusel allí y en el portal del huésped.

**Architecture:** `habitaciones-api` recibe el archivo, valida su contenido real (firma de bytes), lo sube al bucket público `habitaciones` con la clave secreta (que nunca sale del backend) y guarda la URL en una tabla nueva `habitacion_imagenes`. Las lecturas de habitaciones y de `mis-reservas/disponibles` incluyen la lista de imágenes. Los fronts muestran `Carousel` y gestionan con `ImageManager` (ya existen en `hotel-ui`).

**Tech Stack:** ASP.NET Core 8 + EF Core (Npgsql), xUnit + EF Core InMemory para las pruebas del backend, Supabase Storage (API REST), React 18 + `hotel-ui`.

**Spec:** `docs/superpowers/specs/2026-10-07-imagenes-habitaciones-design.md`

## Global Constraints

- Rama de trabajo: `feat/imagenes-habitaciones-impl` (sale de `feat/hotel-ui-libreria-develop`). No se hace `push`; lo hace el usuario.
- Commits con el correo `santirramos@gmail.com` (ya configurado) y **sin** `Co-Authored-By` ni menciones a Claude.
- Nunca imprimir, copiar ni commitear valores de `.env` (`SUPABASE_SECRET_KEY`, contraseñas, `JWT_KEY`). No modificar ningún `.env`. `.env.example` solo lleva marcadores de posición.
- Formatos permitidos: JPG, PNG y WebP, verificados por la firma de bytes del archivo (no por extensión ni por `Content-Type` declarado). Máximo 5 MB (`5 * 1024 * 1024` bytes, el valor exacto se permite) y máximo 6 imágenes por habitación.
- Solo `admin` y `recepcion` pueden subir, borrar y reordenar (`[Authorize(Roles = "admin,recepcion")]`). El huésped solo ve.
- Errores en JSON `{ "message": "..." }`, en español, como el resto de la API. Textos exactos:
  `Selecciona una imagen.` · `La imagen está vacía.` · `La imagen supera 5 MB.` · `Formato no permitido. Usa JPG, PNG o WebP.` · `Máximo 6 imágenes por habitación.` · `No se pueden cambiar las imágenes de una habitación dada de baja.` · `La lista de imágenes no coincide con las de la habitación.` · `No se pudo guardar la imagen. Intenta de nuevo.`
- .NET 8 (`net8.0`). Antes de usar `dotnet` en cada terminal: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH`.
- Dar de baja una habitación no borra sus imágenes.
- Las pruebas contra servicios reales (Supabase) dejan datos de prueba: todo lo que se cree debe borrarse al terminar (Task 11).
- Puertos: auth-api 5001, clientes-api 5002, habitaciones-api 5003, reservas-api 5004; fronts 5173 (auth), 5174 (habitaciones), 5175 (clientes), 5176 (reservas).

## Review Focus

1. Archivo cuyo `Content-Type` y extensión dicen imagen pero cuyo contenido no lo es (HTML, un ejecutable renombrado): debe rechazarse con 400 y no subirse al bucket. (Task 4)
2. Falla del Storage al subir (red o clave inválida): respuesta 502 con mensaje claro y ninguna fila en la base; y si falla el guardado en la base después de subir, el objeto del bucket se borra. (Task 4)
3. Borrar una imagen cuyo objeto ya no existe en el bucket (o cuyo borrado falla): la fila se elimina igual y el usuario no ve un error. (Task 5)
4. Reordenar con ids repetidos, faltantes o de otra habitación: 400 y el orden no cambia. (Task 5)
5. Habitación inexistente o dada de baja, e imagen que pertenece a otra habitación: 404/409 sin tocar nada. (Tasks 4 y 5)

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `db/habitacion_imagenes.sql` (nuevo) | Tabla de imágenes (la ejecuta el usuario en Supabase) |
| `habitaciones-api/Models/HabitacionImagen.cs` (nuevo) | Entidad, `ImagenVista` y DTO de orden |
| `habitaciones-api/Services/FirmaImagen.cs` (nuevo) | Detecta JPG/PNG/WebP por bytes |
| `habitaciones-api/Services/IImagenStorage.cs`, `SupabaseStorage.cs` (nuevos) | Contrato y cliente REST de Supabase Storage, `StorageOptions` |
| `habitaciones-api/Controllers/HabitacionImagenesController.cs` (nuevo) | Subir, borrar, reordenar |
| `habitaciones-api/Controllers/HabitacionesController.cs`, `Models/Habitacion.cs`, `Data/HabitacionesDbContext.cs`, `Program.cs` | Imágenes en lecturas, DbSet y registro de servicios |
| `habitaciones-api.Tests/` (nuevo) | Proyecto xUnit del backend |
| `reservas-api/Models/Reserva.cs`, `Data/ReservasDbContext.cs`, `Controllers/MisReservasController.cs` | Imágenes en `disponibles` |
| `hotel-ui/src/assets.js`, `index.js`, `index.test.js` | Imagen de relleno `SIN_FOTO` |
| `habitaciones-front/src/{api.js,pages/HabitacionesList.jsx,pages/HabitacionForm.jsx,styles.css}` | Carrusel en el listado y gestor en el formulario |
| `reservas-front/src/{pages/Reservar.jsx,styles.css}` | Carrusel en el portal del huésped |
| `README.md` | Pasos de base de datos y variables de Storage |

---

### Task 1: Script SQL, documentación y comprobación de la tabla

**Files:**
- Create: `db/habitacion_imagenes.sql`
- Modify: `README.md`

**Interfaces:**
- Produces: tabla `public.habitacion_imagenes(id uuid, habitacion_id uuid, ruta text, url text, orden int, creado_en timestamptz)`. Las tareas 3 a 7 la usan.

- [ ] **Step 1: Crear `db/habitacion_imagenes.sql`**

```sql
-- Imágenes de habitaciones (carrusel). Ejecutar después de habitaciones.sql.
-- `ruta` es la ruta del objeto en el bucket de Storage (para poder borrarlo); `url` es la URL pública.
create table if not exists public.habitacion_imagenes (
  id uuid primary key default gen_random_uuid(),
  habitacion_id uuid not null references public.habitaciones(id),
  ruta text not null,
  url text not null,
  orden int not null default 0 check (orden >= 0),
  creado_en timestamptz not null default now()
);
create index if not exists habitacion_imagenes_hab_orden on public.habitacion_imagenes (habitacion_id, orden);

-- La API se conecta como dueño de la tabla (no le afecta); esto evita que la API de datos pública de Supabase la exponga.
alter table public.habitacion_imagenes enable row level security;
```

- [ ] **Step 2: Documentar en el `README.md`**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'README.md'
s = open(p, encoding='utf-8').read()

old = "ejecutar `db/usuarios.sql`, `db/clientes.sql` y `db/habitaciones.sql` y `db/reservas.sql`"
assert s.count(old) == 1
s = s.replace(old, "ejecutar `db/usuarios.sql`, `db/clientes.sql` y `db/habitaciones.sql` y `db/habitacion_imagenes.sql` y `db/reservas.sql`")

marker = "### 3. Backends"
assert s.count(marker) == 1
bloque = """### Imágenes de habitaciones (Supabase Storage)
1. En Supabase → Storage → New bucket: nombre `habitaciones`, con **Public bucket** activado (límite 5 MB; tipos `image/jpeg`, `image/png`, `image/webp`).
2. En `habitaciones-api/.env` agregar `SUPABASE_URL` (Settings → API), `SUPABASE_SECRET_KEY` (Settings → API Keys → secret key; **solo backend, nunca en el front ni en git**) y, si el bucket se llama distinto, `SUPABASE_BUCKET`.
3. Sin `SUPABASE_URL` o `SUPABASE_SECRET_KEY`, `habitaciones-api` no arranca y lo dice con un mensaje claro.
4. Pruebas del backend de habitaciones: `dotnet test habitaciones-api.Tests`.

"""
s = s.replace(marker, bloque + marker)
open(p, 'w', encoding='utf-8').write(s)
print('README actualizado')
EOF
```
Expected: `README actualizado`.

- [ ] **Step 3: Comprobar si la tabla ya existe en Supabase (solo lectura)**

Run (no imprimir variables):
```bash
cd habitaciones-api && set -a && . ./.env && set +a && export PGPASSWORD="$SUPABASE_PASSWORD" PGCONNECT_TIMEOUT=20 && psql -h "$SUPABASE_HOST" -p "$SUPABASE_PORT" -U "$SUPABASE_USER" -d "$SUPABASE_DB" -tA -c "select coalesce(to_regclass('public.habitacion_imagenes')::text, 'NO EXISTE')"; cd ..
```
Expected: `habitacion_imagenes` si el usuario ya ejecutó el script; `NO EXISTE` en caso contrario.

- [ ] **Step 4: Si dio `NO EXISTE`, detenerse y pedir al usuario que ejecute el script**

Este es un cambio en la base de datos compartida del usuario: **no se aplica automáticamente**. Decir al usuario: "Ejecuta `db/habitacion_imagenes.sql` en Supabase → SQL Editor y avísame", esperar su respuesta y repetir el Step 3 hasta ver `habitacion_imagenes`. Las tareas 2 a 9 (backend con pruebas en memoria y fronts) no necesitan la tabla real y pueden avanzar mientras tanto; la Task 11 sí.

- [ ] **Step 5: Commit**

```bash
git add db/habitacion_imagenes.sql README.md
git commit -m "feat(db): tabla habitacion_imagenes y documentación de Storage"
```

---

### Task 2: Proyecto de pruebas del backend y `FirmaImagen` (TDD)

**Files:**
- Create: `habitaciones-api.Tests/HabitacionesApi.Tests.csproj`
- Create: `habitaciones-api.Tests/FirmaImagenTests.cs`
- Create: `habitaciones-api/Services/FirmaImagen.cs`

**Interfaces:**
- Produces: `FirmaImagen.Detectar(ReadOnlySpan<byte> cabecera)` → `FirmaImagen.Tipo?` con `ContentType` (`image/jpeg|image/png|image/webp`) y `Extension` (`jpg|png|webp`); `null` si no es una imagen permitida. Namespace `HabitacionesApi.Services`. Comando de pruebas: `dotnet test habitaciones-api.Tests`.

- [ ] **Step 1: Crear `habitaciones-api.Tests/HabitacionesApi.Tests.csproj`**

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
    <ProjectReference Include="../habitaciones-api/HabitacionesApi.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Escribir la prueba que falla, `habitaciones-api.Tests/FirmaImagenTests.cs`**

```csharp
using System.Text;
using HabitacionesApi.Services;

namespace HabitacionesApi.Tests;

public class FirmaImagenTests
{
    private static byte[] Bytes(params int[] valores) => valores.Select(v => (byte)v).ToArray();

    [Fact]
    public void Reconoce_jpeg()
    {
        var t = FirmaImagen.Detectar(Bytes(0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0));
        Assert.Equal("image/jpeg", t?.ContentType);
        Assert.Equal("jpg", t?.Extension);
    }

    [Fact]
    public void Reconoce_png()
    {
        var t = FirmaImagen.Detectar(Bytes(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0));
        Assert.Equal("image/png", t?.ContentType);
        Assert.Equal("png", t?.Extension);
    }

    [Fact]
    public void Reconoce_webp()
    {
        var t = FirmaImagen.Detectar(Bytes('R', 'I', 'F', 'F', 1, 2, 3, 4, 'W', 'E', 'B', 'P'));
        Assert.Equal("image/webp", t?.ContentType);
        Assert.Equal("webp", t?.Extension);
    }

    [Fact]
    public void Rechaza_html_aunque_se_llame_png()
    {
        Assert.Null(FirmaImagen.Detectar(Encoding.ASCII.GetBytes("<html><body>")));
    }

    [Fact]
    public void Rechaza_ejecutable_renombrado()
    {
        Assert.Null(FirmaImagen.Detectar(Bytes(0x4D, 0x5A, 0x90, 0x00, 0x03, 0, 0, 0, 0x04, 0, 0, 0)));
    }

    [Fact]
    public void Rechaza_riff_que_no_es_webp()
    {
        Assert.Null(FirmaImagen.Detectar(Bytes('R', 'I', 'F', 'F', 1, 2, 3, 4, 'W', 'A', 'V', 'E')));
    }

    [Fact]
    public void Rechaza_vacio_y_cabeceras_incompletas()
    {
        Assert.Null(FirmaImagen.Detectar(Array.Empty<byte>()));
        Assert.Null(FirmaImagen.Detectar(Bytes(0xFF, 0xD8)));
        Assert.Null(FirmaImagen.Detectar(Bytes(0x89, 0x50, 0x4E, 0x47)));
    }
}
```

- [ ] **Step 3: Ejecutar y comprobar que falla**

Run: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH; dotnet test habitaciones-api.Tests 2>&1 | tail -15`
Expected: falla de compilación `error CS0103` o `CS0246` mencionando `FirmaImagen` (la clase no existe). Si la restauración de paquetes falla por red, repetirlo.

- [ ] **Step 4: Implementar `habitaciones-api/Services/FirmaImagen.cs`**

```csharp
namespace HabitacionesApi.Services;

// Identifica el formato real de una imagen por sus primeros bytes. No se confía en la extensión ni en el Content-Type.
public static class FirmaImagen
{
    public sealed record Tipo(string ContentType, string Extension);

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // `cabecera` debe traer al menos los primeros 12 bytes del archivo (si el archivo es más corto, los que tenga).
    public static Tipo? Detectar(ReadOnlySpan<byte> cabecera)
    {
        if (cabecera.Length >= 3 && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF)
            return new Tipo("image/jpeg", "jpg");

        if (cabecera.Length >= Png.Length && cabecera[..Png.Length].SequenceEqual(Png))
            return new Tipo("image/png", "png");

        if (cabecera.Length >= 12
            && cabecera[0] == 'R' && cabecera[1] == 'I' && cabecera[2] == 'F' && cabecera[3] == 'F'
            && cabecera[8] == 'W' && cabecera[9] == 'E' && cabecera[10] == 'B' && cabecera[11] == 'P')
            return new Tipo("image/webp", "webp");

        return null;
    }
}
```

- [ ] **Step 5: Ejecutar y comprobar que pasa**

Run: `dotnet test habitaciones-api.Tests 2>&1 | tail -6`
Expected: `Passed!  - Failed: 0, Passed: 7`.

- [ ] **Step 6: Commit**

```bash
git add habitaciones-api.Tests habitaciones-api/Services/FirmaImagen.cs
git commit -m "feat(habitaciones-api): detección de formato de imagen por firma de bytes y proyecto de pruebas"
```

---

### Task 3: Modelo, almacenamiento en Supabase y configuración (TDD)

**Files:**
- Create: `habitaciones-api/Models/HabitacionImagen.cs`
- Create: `habitaciones-api/Services/IImagenStorage.cs`
- Create: `habitaciones-api/Services/SupabaseStorage.cs`
- Create: `habitaciones-api.Tests/StorageTests.cs`
- Modify: `habitaciones-api/Models/Habitacion.cs`
- Modify: `habitaciones-api/Data/HabitacionesDbContext.cs`
- Modify: `habitaciones-api/Program.cs`

**Interfaces:**
- Produces:
  - `HabitacionImagen` (entidad), `ImagenVista(Guid Id, string Url, int Orden)`, `OrdenImagenesDto { List<Guid> Ids }` (namespace `HabitacionesApi.Models`); `Habitacion.Imagenes` (`[NotMapped] List<ImagenVista>`); `HabitacionesDbContext.HabitacionImagenes`.
  - `IImagenStorage { Task SubirAsync(string ruta, Stream contenido, string contentType, CancellationToken ct); Task BorrarAsync(string ruta, CancellationToken ct); string UrlPublica(string ruta); }`, `ImagenStorageException(string message, Exception? inner = null)`, `StorageOptions(string Url, string SecretKey, string Bucket)` con `StorageOptions.Desde(Func<string, string?> leer)`, `SupabaseStorage` (namespace `HabitacionesApi.Services`). `BorrarAsync` no falla si el objeto no existe.

- [ ] **Step 1: Escribir las pruebas que fallan, `habitaciones-api.Tests/StorageTests.cs`**

```csharp
using HabitacionesApi.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HabitacionesApi.Tests;

public class StorageTests
{
    private static Func<string, string?> Entorno(Dictionary<string, string?> valores) =>
        nombre => valores.TryGetValue(nombre, out var v) ? v : null;

    [Fact]
    public void Opciones_exigen_url_y_clave()
    {
        var e1 = Assert.Throws<InvalidOperationException>(() => StorageOptions.Desde(Entorno(new() { ["SUPABASE_SECRET_KEY"] = "k" })));
        Assert.Contains("SUPABASE_URL", e1.Message);
        var e2 = Assert.Throws<InvalidOperationException>(() => StorageOptions.Desde(Entorno(new() { ["SUPABASE_URL"] = "https://x.supabase.co", ["SUPABASE_SECRET_KEY"] = "  " })));
        Assert.Contains("SUPABASE_SECRET_KEY", e2.Message);
    }

    [Fact]
    public void Opciones_usan_bucket_por_defecto_y_quitan_la_barra_final()
    {
        var o = StorageOptions.Desde(Entorno(new() { ["SUPABASE_URL"] = " https://x.supabase.co/ ", ["SUPABASE_SECRET_KEY"] = "k" }));
        Assert.Equal("https://x.supabase.co", o.Url);
        Assert.Equal("habitaciones", o.Bucket);
    }

    [Fact]
    public void Opciones_respetan_un_bucket_propio()
    {
        var o = StorageOptions.Desde(Entorno(new() { ["SUPABASE_URL"] = "https://x.supabase.co", ["SUPABASE_SECRET_KEY"] = "k", ["SUPABASE_BUCKET"] = "fotos" }));
        Assert.Equal("fotos", o.Bucket);
    }

    [Fact]
    public void La_url_publica_apunta_al_bucket()
    {
        var s = new SupabaseStorage(new HttpClient(), new StorageOptions("https://x.supabase.co", "k", "habitaciones"), NullLogger<SupabaseStorage>.Instance);
        Assert.Equal("https://x.supabase.co/storage/v1/object/public/habitaciones/abc/def.png", s.UrlPublica("abc/def.png"));
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH; dotnet test habitaciones-api.Tests 2>&1 | grep -E "error CS|Passed|Failed" | head -5`
Expected: errores de compilación por `StorageOptions` y `SupabaseStorage` inexistentes.

- [ ] **Step 3: Crear `habitaciones-api/Models/HabitacionImagen.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HabitacionesApi.Models;

[Table("habitacion_imagenes", Schema = "public")]
public class HabitacionImagen
{
    [Column("id")] public Guid Id { get; set; }
    [Column("habitacion_id")] public Guid HabitacionId { get; set; }
    [Column("ruta")] public string Ruta { get; set; } = "";
    [Column("url")] public string Url { get; set; } = "";
    [Column("orden")] public int Orden { get; set; }
    [Column("creado_en")] public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

// Lo que ve el cliente de cada imagen
public record ImagenVista(Guid Id, string Url, int Orden);

public class OrdenImagenesDto
{
    [Required] public List<Guid> Ids { get; set; } = [];
}
```

- [ ] **Step 4: Modificar `Habitacion.cs` y `HabitacionesDbContext.cs`**

En `habitaciones-api/Models/Habitacion.cs`, dentro de la clase `Habitacion`, agregar después de la línea `[Column("creado_en")] public DateTime CreadoEn { get; set; } = DateTime.UtcNow;`:

```csharp
    // Se rellena al leer (no es una columna); lo usan el listado y el detalle
    [NotMapped] public List<ImagenVista> Imagenes { get; set; } = [];
```

`habitaciones-api/Data/HabitacionesDbContext.cs` completo:

```csharp
using HabitacionesApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HabitacionesApi.Data;

public class HabitacionesDbContext(DbContextOptions<HabitacionesDbContext> options) : DbContext(options)
{
    public DbSet<Habitacion> Habitaciones => Set<Habitacion>();
    public DbSet<HabitacionImagen> HabitacionImagenes => Set<HabitacionImagen>();
}
```

- [ ] **Step 5: Crear `habitaciones-api/Services/IImagenStorage.cs`**

```csharp
namespace HabitacionesApi.Services;

public interface IImagenStorage
{
    Task SubirAsync(string ruta, Stream contenido, string contentType, CancellationToken ct);
    // No falla si el objeto ya no existe
    Task BorrarAsync(string ruta, CancellationToken ct);
    string UrlPublica(string ruta);
}

public class ImagenStorageException(string message, Exception? inner = null) : Exception(message, inner);
```

- [ ] **Step 6: Crear `habitaciones-api/Services/SupabaseStorage.cs`**

```csharp
using System.Net;
using System.Net.Http.Headers;

namespace HabitacionesApi.Services;

public record StorageOptions(string Url, string SecretKey, string Bucket)
{
    // `leer` devuelve el valor de una variable de entorno (o null)
    public static StorageOptions Desde(Func<string, string?> leer)
    {
        var url = leer("SUPABASE_URL");
        var clave = leer("SUPABASE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(clave))
            throw new InvalidOperationException(
                "Faltan SUPABASE_URL o SUPABASE_SECRET_KEY (Storage de imágenes). Copia .env.example como .env y complétalo.");
        var bucket = leer("SUPABASE_BUCKET");
        return new StorageOptions(url.Trim().TrimEnd('/'), clave.Trim(), string.IsNullOrWhiteSpace(bucket) ? "habitaciones" : bucket.Trim());
    }
}

// Cliente mínimo de la API REST de Supabase Storage. La clave secreta solo viaja del backend a Supabase.
public class SupabaseStorage(HttpClient http, StorageOptions opciones, ILogger<SupabaseStorage> log) : IImagenStorage
{
    public string UrlPublica(string ruta) => $"{opciones.Url}/storage/v1/object/public/{opciones.Bucket}/{ruta}";

    private string UrlObjeto(string ruta) => $"{opciones.Url}/storage/v1/object/{opciones.Bucket}/{ruta}";

    private HttpRequestMessage Peticion(HttpMethod metodo, string ruta)
    {
        var req = new HttpRequestMessage(metodo, UrlObjeto(ruta));
        req.Headers.Add("apikey", opciones.SecretKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opciones.SecretKey);
        return req;
    }

    public async Task SubirAsync(string ruta, Stream contenido, string contentType, CancellationToken ct)
    {
        using var req = Peticion(HttpMethod.Post, ruta);
        req.Content = new StreamContent(contenido);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var res = await Enviar(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            log.LogError("Storage rechazó la subida de {Ruta}: {Estado}", ruta, (int)res.StatusCode);
            throw new ImagenStorageException($"Storage respondió {(int)res.StatusCode} al subir la imagen.");
        }
    }

    public async Task BorrarAsync(string ruta, CancellationToken ct)
    {
        using var req = Peticion(HttpMethod.Delete, ruta);
        using var res = await Enviar(req, ct);
        if (res.StatusCode == HttpStatusCode.NotFound) return; // ya no existe: el resultado buscado
        if (!res.IsSuccessStatusCode)
        {
            log.LogWarning("Storage rechazó el borrado de {Ruta}: {Estado}", ruta, (int)res.StatusCode);
            throw new ImagenStorageException($"Storage respondió {(int)res.StatusCode} al borrar la imagen.");
        }
    }

    private async Task<HttpResponseMessage> Enviar(HttpRequestMessage req, CancellationToken ct)
    {
        try { return await http.SendAsync(req, ct); }
        catch (HttpRequestException e) { throw new ImagenStorageException("No se pudo conectar con Storage.", e); }
    }
}
```

- [ ] **Step 7: Registrar en `Program.cs`**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'habitaciones-api/Program.cs'
s = open(p, encoding='utf-8').read()

old_using = "using HabitacionesApi.Data;\n"
assert s.startswith(old_using)
s = s.replace(old_using, "using HabitacionesApi.Data;\nusing HabitacionesApi.Services;\n", 1)

anchor = "b.Services.AddDbContext<HabitacionesDbContext>(o => o.UseNpgsql(connStr));\n"
assert s.count(anchor) == 1
nuevo = anchor + """// Storage de imágenes: si faltan SUPABASE_URL o SUPABASE_SECRET_KEY, falla al arrancar con un mensaje claro
b.Services.AddSingleton(StorageOptions.Desde(Environment.GetEnvironmentVariable));
b.Services.AddHttpClient<IImagenStorage, SupabaseStorage>();
"""
s = s.replace(anchor, nuevo, 1)
open(p, 'w', encoding='utf-8').write(s)
print('Program.cs actualizado')
EOF
```
Expected: `Program.cs actualizado`.

- [ ] **Step 8: Ejecutar y comprobar que pasa**

Run: `dotnet build habitaciones-api 2>&1 | grep -E "error|Warn|Error" | head -5; dotnet test habitaciones-api.Tests 2>&1 | tail -4`
Expected: `0 Error(s)` y `Passed!  - Failed: 0, Passed: 11` (7 + 4).

- [ ] **Step 9: Commit**

```bash
git add habitaciones-api habitaciones-api.Tests
git commit -m "feat(habitaciones-api): modelo de imágenes y cliente de Supabase Storage"
```

---

### Task 4: Subir imágenes (TDD)

**Files:**
- Create: `habitaciones-api.Tests/Ayudas.cs`
- Create: `habitaciones-api.Tests/SubirImagenTests.cs`
- Create: `habitaciones-api/Controllers/HabitacionImagenesController.cs`

**Interfaces:**
- Consumes: `FirmaImagen`, `IImagenStorage`, `ImagenStorageException`, `HabitacionImagen`, `ImagenVista`, `HabitacionesDbContext.HabitacionImagenes` (Tasks 2 y 3).
- Produces: `HabitacionImagenesController(HabitacionesDbContext db, IImagenStorage storage, ILogger<HabitacionImagenesController> log)` con constantes públicas `MaxImagenes = 6` y `MaxBytes = 5 * 1024 * 1024`, y `Task<IActionResult> Subir(Guid habitacionId, IFormFile? archivo, CancellationToken ct)` (`POST api/habitaciones/{habitacionId}/imagenes`, campo `archivo`; 201 con `ImagenVista`). La Task 5 añade `Borrar` y `Reordenar` a esta misma clase. Ayudas de prueba (`StorageFalso`, `Datos`) que usan las Tasks 5 y 6.

- [ ] **Step 1: Crear las ayudas de prueba, `habitaciones-api.Tests/Ayudas.cs`**

```csharp
using HabitacionesApi.Data;
using HabitacionesApi.Models;
using HabitacionesApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitacionesApi.Tests;

public sealed class StorageFalso : IImagenStorage
{
    public List<string> Subidos { get; } = [];
    public List<string> Borrados { get; } = [];
    public bool FallaAlSubir { get; set; }
    public bool FallaAlBorrar { get; set; }

    public Task SubirAsync(string ruta, Stream contenido, string contentType, CancellationToken ct)
    {
        if (FallaAlSubir) throw new ImagenStorageException("falló");
        Subidos.Add(ruta);
        return Task.CompletedTask;
    }

    public Task BorrarAsync(string ruta, CancellationToken ct)
    {
        if (FallaAlBorrar) throw new ImagenStorageException("falló");
        Borrados.Add(ruta);
        return Task.CompletedTask;
    }

    public string UrlPublica(string ruta) => $"https://cdn.test/{ruta}";
}

// Base de datos que falla al guardar, para probar la compensación
public sealed class DbQueFalla(DbContextOptions<HabitacionesDbContext> o) : HabitacionesDbContext(o)
{
    public bool Falla { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Falla ? throw new InvalidOperationException("fallo de base de datos") : base.SaveChangesAsync(cancellationToken);
}

public static class Datos
{
    public static DbContextOptions<HabitacionesDbContext> Opciones(string? nombre = null) =>
        new DbContextOptionsBuilder<HabitacionesDbContext>().UseInMemoryDatabase(nombre ?? Guid.NewGuid().ToString()).Options;

    public static HabitacionesDbContext NuevoDb() => new(Opciones());

    public static Habitacion Habitacion(HabitacionesDbContext db, bool activo = true)
    {
        var h = new Habitacion { Id = Guid.NewGuid(), Numero = Guid.NewGuid().ToString("N")[..6], Activo = activo };
        db.Habitaciones.Add(h);
        db.SaveChanges();
        return h;
    }

    public static HabitacionImagen Imagen(HabitacionesDbContext db, Guid habitacionId, int orden, string? ruta = null)
    {
        ruta ??= $"{habitacionId}/{Guid.NewGuid()}.png";
        var i = new HabitacionImagen
        {
            Id = Guid.NewGuid(), HabitacionId = habitacionId, Ruta = ruta,
            Url = $"https://cdn.test/{ruta}", Orden = orden, CreadoEn = DateTime.UtcNow.AddSeconds(orden),
        };
        db.HabitacionImagenes.Add(i);
        db.SaveChanges();
        return i;
    }

    public static IFormFile Archivo(byte[] contenido, string nombre = "foto.png", string tipo = "image/png")
    {
        var ms = new MemoryStream(contenido);
        return new FormFile(ms, 0, ms.Length, "archivo", nombre) { Headers = new HeaderDictionary(), ContentType = tipo };
    }

    // PNG mínimo válido en la cabecera, relleno de ceros hasta `total` bytes
    public static byte[] Png(int total = 64)
    {
        var b = new byte[total];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(b, 0);
        return b;
    }

    public static string Mensaje(IActionResult r)
    {
        var valor = ((ObjectResult)r).Value!;
        return (string)valor.GetType().GetProperty("message")!.GetValue(valor)!;
    }
}
```

- [ ] **Step 2: Escribir las pruebas que fallan, `habitaciones-api.Tests/SubirImagenTests.cs`**

```csharp
using System.Text;
using HabitacionesApi.Controllers;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HabitacionesApi.Tests;

public class SubirImagenTests
{
    private static HabitacionImagenesController Crear(Data.HabitacionesDbContext db, StorageFalso st) =>
        new(db, st, NullLogger<HabitacionImagenesController>.Instance);

    [Fact]
    public async Task Sube_un_png_valido_y_guarda_la_url()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default);

        var creado = Assert.IsType<ObjectResult>(r);
        Assert.Equal(201, creado.StatusCode);
        var vista = Assert.IsType<ImagenVista>(creado.Value);
        Assert.Equal(0, vista.Orden);
        var ruta = Assert.Single(st.Subidos);
        Assert.StartsWith($"{h.Id}/", ruta);
        Assert.EndsWith(".png", ruta);
        var fila = await db.HabitacionImagenes.SingleAsync();
        Assert.Equal(st.UrlPublica(ruta), fila.Url);
        Assert.Equal(ruta, fila.Ruta);
    }

    [Fact]
    public async Task La_segunda_imagen_queda_al_final()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        Datos.Imagen(db, h.Id, 0);
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default);
        Assert.Equal(1, Assert.IsType<ImagenVista>(((ObjectResult)r).Value).Orden);
    }

    [Fact]
    public async Task Usa_el_tipo_real_y_no_el_declarado()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png(), "cosa.txt", "text/plain"), default);
        Assert.Equal(201, ((ObjectResult)r).StatusCode);
        Assert.EndsWith(".png", Assert.Single(st.Subidos));
    }

    [Fact]
    public async Task Sin_archivo_es_400()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db);
        var r = await Crear(db, new StorageFalso()).Subir(h.Id, null, default);
        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal("Selecciona una imagen.", Datos.Mensaje(r));
    }

    [Fact]
    public async Task Archivo_vacio_es_400()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo([]), default);
        Assert.Equal("La imagen está vacía.", Datos.Mensaje(r));
        Assert.Empty(st.Subidos);
    }

    [Fact]
    public async Task Mas_de_5_MB_es_400_y_exactamente_5_MB_se_permite()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var c = Crear(db, st);
        var grande = await c.Subir(h.Id, Datos.Archivo(Datos.Png((int)HabitacionImagenesController.MaxBytes + 1)), default);
        Assert.IsType<BadRequestObjectResult>(grande);
        Assert.Equal("La imagen supera 5 MB.", Datos.Mensaje(grande));
        Assert.Empty(st.Subidos);

        var justa = await c.Subir(h.Id, Datos.Archivo(Datos.Png((int)HabitacionImagenesController.MaxBytes)), default);
        Assert.Equal(201, ((ObjectResult)justa).StatusCode);
    }

    // Review Focus 1
    [Fact]
    public async Task Rechaza_contenido_que_no_es_imagen_aunque_diga_png()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var falso = Datos.Archivo(Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"), "foto.png", "image/png");
        var r = await Crear(db, st).Subir(h.Id, falso, default);
        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal("Formato no permitido. Usa JPG, PNG o WebP.", Datos.Mensaje(r));
        Assert.Empty(st.Subidos);
        Assert.Empty(db.HabitacionImagenes);
    }

    [Fact]
    public async Task La_septima_imagen_es_409()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        for (var i = 0; i < HabitacionImagenesController.MaxImagenes; i++) Datos.Imagen(db, h.Id, i);
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default);
        Assert.IsType<ConflictObjectResult>(r);
        Assert.Equal("Máximo 6 imágenes por habitación.", Datos.Mensaje(r));
        Assert.Empty(st.Subidos);
    }

    // Review Focus 5
    [Fact]
    public async Task Habitacion_inexistente_es_404_y_dada_de_baja_es_409()
    {
        var db = Datos.NuevoDb(); var baja = Datos.Habitacion(db, activo: false); var st = new StorageFalso();
        var c = Crear(db, st);
        Assert.IsType<NotFoundResult>(await c.Subir(Guid.NewGuid(), Datos.Archivo(Datos.Png()), default));
        var r = await c.Subir(baja.Id, Datos.Archivo(Datos.Png()), default);
        Assert.IsType<ConflictObjectResult>(r);
        Assert.Equal("No se pueden cambiar las imágenes de una habitación dada de baja.", Datos.Mensaje(r));
        Assert.Empty(st.Subidos);
    }

    // Review Focus 2
    [Fact]
    public async Task Si_Storage_falla_es_502_y_no_queda_fila()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso { FallaAlSubir = true };
        var r = await Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default);
        Assert.Equal(502, ((ObjectResult)r).StatusCode);
        Assert.Equal("No se pudo guardar la imagen. Intenta de nuevo.", Datos.Mensaje(r));
        Assert.Empty(db.HabitacionImagenes);
    }

    // Review Focus 2
    [Fact]
    public async Task Si_falla_la_base_despues_de_subir_se_borra_el_objeto()
    {
        var opciones = Datos.Opciones();
        var db = new DbQueFalla(opciones);
        var h = Datos.Habitacion(db);
        var st = new StorageFalso();
        db.Falla = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Crear(db, st).Subir(h.Id, Datos.Archivo(Datos.Png()), default));
        Assert.Equal(st.Subidos, st.Borrados);
        Assert.Single(st.Borrados);
    }

    [Fact]
    public void Solo_admin_y_recepcion_pueden_gestionar_imagenes()
    {
        var atributo = typeof(HabitacionImagenesController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();
        Assert.Equal("admin,recepcion", atributo.Roles);
    }
}
```

Nota: `Si_falla_la_base_despues_de_subir...` siembra la habitación con `Falla` apagado y la enciende justo antes del `Subir`; `Datos.Habitacion` usa `db.SaveChanges()` (síncrono), que `DbQueFalla` no intercepta.

- [ ] **Step 3: Ejecutar y comprobar que falla**

Run: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH; dotnet test habitaciones-api.Tests 2>&1 | grep -E "error CS|Passed|Failed" | head -5`
Expected: errores de compilación por `HabitacionImagenesController` inexistente.

- [ ] **Step 4: Implementar `habitaciones-api/Controllers/HabitacionImagenesController.cs`**

```csharp
using HabitacionesApi.Data;
using HabitacionesApi.Models;
using HabitacionesApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitacionesApi.Controllers;

[ApiController]
[Authorize(Roles = "admin,recepcion")]
[Route("api/habitaciones/{habitacionId:guid}/imagenes")]
public class HabitacionImagenesController(HabitacionesDbContext db, IImagenStorage storage,
    ILogger<HabitacionImagenesController> log) : ControllerBase
{
    public const int MaxImagenes = 6;
    public const long MaxBytes = 5 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)] // el archivo más el sobrecosto del multipart
    public async Task<IActionResult> Subir(Guid habitacionId, IFormFile? archivo, CancellationToken ct)
    {
        var error = await ValidarHabitacion(habitacionId, ct);
        if (error is not null) return error;
        if (archivo is null) return BadRequest(new { message = "Selecciona una imagen." });
        if (archivo.Length == 0) return BadRequest(new { message = "La imagen está vacía." });
        if (archivo.Length > MaxBytes) return BadRequest(new { message = "La imagen supera 5 MB." });

        await using var contenido = archivo.OpenReadStream();
        var cabecera = new byte[12];
        var leidos = await contenido.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false, ct);
        var tipo = FirmaImagen.Detectar(cabecera.AsSpan(0, leidos));
        if (tipo is null) return BadRequest(new { message = "Formato no permitido. Usa JPG, PNG o WebP." });

        var ordenes = await db.HabitacionImagenes.Where(i => i.HabitacionId == habitacionId)
            .Select(i => i.Orden).ToListAsync(ct);
        if (ordenes.Count >= MaxImagenes)
            return Conflict(new { message = $"Máximo {MaxImagenes} imágenes por habitación." });

        contenido.Position = 0;
        var ruta = $"{habitacionId}/{Guid.NewGuid()}.{tipo.Extension}";
        try { await storage.SubirAsync(ruta, contenido, tipo.ContentType, ct); }
        catch (ImagenStorageException e)
        {
            log.LogError(e, "No se pudo subir la imagen de la habitación {Habitacion}", habitacionId);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "No se pudo guardar la imagen. Intenta de nuevo." });
        }

        var img = new HabitacionImagen
        {
            Id = Guid.NewGuid(), HabitacionId = habitacionId, Ruta = ruta, Url = storage.UrlPublica(ruta),
            Orden = ordenes.Count == 0 ? 0 : ordenes.Max() + 1, CreadoEn = DateTime.UtcNow,
        };
        db.HabitacionImagenes.Add(img);
        try { await db.SaveChangesAsync(ct); }
        catch { await IntentarBorrar(ruta); throw; } // no dejar un objeto huérfano en el bucket
        return StatusCode(StatusCodes.Status201Created, new ImagenVista(img.Id, img.Url, img.Orden));
    }

    // 404 si no existe, 409 si está dada de baja; null si se puede modificar
    private async Task<IActionResult?> ValidarHabitacion(Guid id, CancellationToken ct)
    {
        var h = await db.Habitaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();
        if (!h.Activo) return Conflict(new { message = "No se pueden cambiar las imágenes de una habitación dada de baja." });
        return null;
    }

    private async Task IntentarBorrar(string ruta)
    {
        try { await storage.BorrarAsync(ruta, CancellationToken.None); }
        catch (ImagenStorageException e) { log.LogWarning(e, "No se pudo borrar el objeto {Ruta} del bucket", ruta); }
    }
}
```

- [ ] **Step 5: Ejecutar y comprobar que pasa**

Run: `dotnet test habitaciones-api.Tests 2>&1 | tail -5`
Expected: `Passed!  - Failed: 0, Passed: 23` (11 + 12). Si falla `Si_falla_la_base_despues_de_subir_se_borra_el_objeto` por una excepción distinta de `InvalidOperationException`, revisar que `DbQueFalla.Falla` se active después de sembrar la habitación.

- [ ] **Step 6: Commit**

```bash
git add habitaciones-api habitaciones-api.Tests
git commit -m "feat(habitaciones-api): subir imágenes de habitaciones con validación de contenido"
```

---

### Task 5: Borrar y reordenar imágenes (TDD)

**Files:**
- Create: `habitaciones-api.Tests/BorrarReordenarTests.cs`
- Modify: `habitaciones-api/Controllers/HabitacionImagenesController.cs`

**Interfaces:**
- Consumes: la clase y las ayudas de la Task 4.
- Produces: en `HabitacionImagenesController`: `Task<IActionResult> Borrar(Guid habitacionId, Guid imagenId, CancellationToken ct)` (`DELETE .../imagenes/{imagenId}`; 204) y `Task<IActionResult> Reordenar(Guid habitacionId, OrdenImagenesDto dto, CancellationToken ct)` (`PUT .../imagenes/orden` con `{ "ids": [...] }`; 200 con la lista de `ImagenVista` en el nuevo orden).

- [ ] **Step 1: Escribir las pruebas que fallan, `habitaciones-api.Tests/BorrarReordenarTests.cs`**

```csharp
using HabitacionesApi.Controllers;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HabitacionesApi.Tests;

public class BorrarReordenarTests
{
    private static HabitacionImagenesController Crear(Data.HabitacionesDbContext db, StorageFalso st) =>
        new(db, st, NullLogger<HabitacionImagenesController>.Instance);

    [Fact]
    public async Task Borrar_quita_la_fila_borra_el_objeto_y_compacta_el_orden()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var a = Datos.Imagen(db, h.Id, 0); var b = Datos.Imagen(db, h.Id, 1); var c = Datos.Imagen(db, h.Id, 2);

        var r = await Crear(db, st).Borrar(h.Id, b.Id, default);

        Assert.IsType<NoContentResult>(r);
        Assert.Equal(new[] { b.Ruta }, st.Borrados);
        var restantes = await db.HabitacionImagenes.OrderBy(i => i.Orden).ToListAsync();
        Assert.Equal(new[] { a.Id, c.Id }, restantes.Select(i => i.Id));
        Assert.Equal(new[] { 0, 1 }, restantes.Select(i => i.Orden));
    }

    // Review Focus 3
    [Fact]
    public async Task Borrar_funciona_aunque_Storage_falle_o_el_objeto_ya_no_exista()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso { FallaAlBorrar = true };
        var a = Datos.Imagen(db, h.Id, 0);

        var r = await Crear(db, st).Borrar(h.Id, a.Id, default);

        Assert.IsType<NoContentResult>(r);
        Assert.Empty(db.HabitacionImagenes);
    }

    // Review Focus 5
    [Fact]
    public async Task Borrar_una_imagen_de_otra_habitacion_es_404_y_no_toca_nada()
    {
        var db = Datos.NuevoDb(); var h1 = Datos.Habitacion(db); var h2 = Datos.Habitacion(db); var st = new StorageFalso();
        var ajena = Datos.Imagen(db, h2.Id, 0);

        var r = await Crear(db, st).Borrar(h1.Id, ajena.Id, default);

        Assert.IsType<NotFoundResult>(r);
        Assert.Single(db.HabitacionImagenes);
        Assert.Empty(st.Borrados);
    }

    [Fact]
    public async Task Borrar_en_habitacion_inexistente_es_404_y_en_una_dada_de_baja_es_409()
    {
        var db = Datos.NuevoDb(); var baja = Datos.Habitacion(db, activo: false); var st = new StorageFalso();
        var img = Datos.Imagen(db, baja.Id, 0);
        var c = Crear(db, st);

        Assert.IsType<NotFoundResult>(await c.Borrar(Guid.NewGuid(), img.Id, default));
        Assert.IsType<ConflictObjectResult>(await c.Borrar(baja.Id, img.Id, default));
        Assert.Single(db.HabitacionImagenes);
    }

    [Fact]
    public async Task Reordenar_asigna_el_nuevo_orden()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var st = new StorageFalso();
        var a = Datos.Imagen(db, h.Id, 0); var b = Datos.Imagen(db, h.Id, 1); var c = Datos.Imagen(db, h.Id, 2);

        var r = await Crear(db, st).Reordenar(h.Id, new OrdenImagenesDto { Ids = [c.Id, a.Id, b.Id] }, default);

        var lista = Assert.IsAssignableFrom<IEnumerable<ImagenVista>>(Assert.IsType<OkObjectResult>(r).Value).ToList();
        Assert.Equal(new[] { c.Id, a.Id, b.Id }, lista.Select(i => i.Id));
        Assert.Equal(new[] { 0, 1, 2 }, lista.Select(i => i.Orden));
        var enBase = await db.HabitacionImagenes.OrderBy(i => i.Orden).Select(i => i.Id).ToListAsync();
        Assert.Equal(new[] { c.Id, a.Id, b.Id }, enBase);
    }

    // Review Focus 4
    [Theory]
    [InlineData("faltante")]
    [InlineData("repetido")]
    [InlineData("ajeno")]
    [InlineData("sobrante")]
    public async Task Reordenar_con_una_lista_que_no_coincide_es_400_y_no_cambia_nada(string caso)
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db); var otra = Datos.Habitacion(db); var st = new StorageFalso();
        var a = Datos.Imagen(db, h.Id, 0); var b = Datos.Imagen(db, h.Id, 1);
        var ajena = Datos.Imagen(db, otra.Id, 0);
        List<Guid> ids = caso switch
        {
            "faltante" => [a.Id],
            "repetido" => [a.Id, a.Id],
            "ajeno" => [a.Id, ajena.Id],
            _ => [a.Id, b.Id, Guid.NewGuid()],
        };

        var r = await Crear(db, st).Reordenar(h.Id, new OrdenImagenesDto { Ids = ids }, default);

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal("La lista de imágenes no coincide con las de la habitación.", Datos.Mensaje(r));
        Assert.Equal(0, (await db.HabitacionImagenes.FindAsync(a.Id))!.Orden);
        Assert.Equal(1, (await db.HabitacionImagenes.FindAsync(b.Id))!.Orden);
    }

    [Fact]
    public async Task Reordenar_en_habitacion_inexistente_es_404_y_en_una_dada_de_baja_es_409()
    {
        var db = Datos.NuevoDb(); var baja = Datos.Habitacion(db, activo: false); var st = new StorageFalso();
        var c = Crear(db, st);
        Assert.IsType<NotFoundResult>(await c.Reordenar(Guid.NewGuid(), new OrdenImagenesDto(), default));
        Assert.IsType<ConflictObjectResult>(await c.Reordenar(baja.Id, new OrdenImagenesDto(), default));
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH; dotnet test habitaciones-api.Tests 2>&1 | grep -E "error CS|Passed|Failed" | head -5`
Expected: errores de compilación `CS1061` (`Borrar` y `Reordenar` no existen en el controlador).

- [ ] **Step 3: Agregar `Borrar` y `Reordenar` al controlador**

En `habitaciones-api/Controllers/HabitacionImagenesController.cs`, insertar estos dos métodos justo antes del comentario `// 404 si no existe, 409 si está dada de baja; null si se puede modificar`:

```csharp
    [HttpDelete("{imagenId:guid}")]
    public async Task<IActionResult> Borrar(Guid habitacionId, Guid imagenId, CancellationToken ct)
    {
        var error = await ValidarHabitacion(habitacionId, ct);
        if (error is not null) return error;
        var img = await db.HabitacionImagenes.FirstOrDefaultAsync(i => i.Id == imagenId && i.HabitacionId == habitacionId, ct);
        if (img is null) return NotFound();

        db.HabitacionImagenes.Remove(img);
        await db.SaveChangesAsync(ct);
        await IntentarBorrar(img.Ruta); // si falla o ya no existe, la fila ya no está: un objeto huérfano es inocuo

        var restantes = await db.HabitacionImagenes.Where(i => i.HabitacionId == habitacionId)
            .OrderBy(i => i.Orden).ThenBy(i => i.CreadoEn).ToListAsync(ct);
        for (var n = 0; n < restantes.Count; n++) restantes[n].Orden = n;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("orden")]
    public async Task<IActionResult> Reordenar(Guid habitacionId, OrdenImagenesDto dto, CancellationToken ct)
    {
        var error = await ValidarHabitacion(habitacionId, ct);
        if (error is not null) return error;
        var imagenes = await db.HabitacionImagenes.Where(i => i.HabitacionId == habitacionId).ToListAsync(ct);

        // La lista debe ser exactamente una permutación de las imágenes de la habitación
        var actuales = imagenes.Select(i => i.Id).OrderBy(x => x).ToList();
        var pedidos = dto.Ids.OrderBy(x => x).ToList();
        if (!actuales.SequenceEqual(pedidos))
            return BadRequest(new { message = "La lista de imágenes no coincide con las de la habitación." });

        for (var n = 0; n < dto.Ids.Count; n++) imagenes.First(i => i.Id == dto.Ids[n]).Orden = n;
        await db.SaveChangesAsync(ct);
        return Ok(imagenes.OrderBy(i => i.Orden).Select(i => new ImagenVista(i.Id, i.Url, i.Orden)).ToList());
    }

```

- [ ] **Step 4: Ejecutar y comprobar que pasa**

Run: `dotnet test habitaciones-api.Tests 2>&1 | tail -5`
Expected: `Passed!  - Failed: 0, Passed: 33` (23 + 10: 4 de borrar, 1 de reordenar, 4 casos de la teoría y 1 de reordenar en habitación inexistente/baja).

- [ ] **Step 5: Commit**

```bash
git add habitaciones-api habitaciones-api.Tests
git commit -m "feat(habitaciones-api): borrar y reordenar imágenes de habitaciones"
```

---

### Task 6: Las lecturas de habitaciones incluyen sus imágenes (TDD)

**Files:**
- Create: `habitaciones-api.Tests/LecturasTests.cs`
- Modify: `habitaciones-api/Controllers/HabitacionesController.cs`

**Interfaces:**
- Consumes: `Habitacion.Imagenes`, `HabitacionImagen`, `ImagenVista`, ayudas de la Task 4.
- Produces: `GET /api/habitaciones` (cada elemento de `items`) y `GET /api/habitaciones/{id}` devuelven `imagenes: [{ id, url, orden }]` ordenadas por `orden` (lista vacía si no hay). Los fronts la consumen en las Tasks 9 y 10.

- [ ] **Step 1: Escribir las pruebas que fallan, `habitaciones-api.Tests/LecturasTests.cs`**

```csharp
using HabitacionesApi.Controllers;
using HabitacionesApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabitacionesApi.Tests;

public class LecturasTests
{
    private static List<Habitacion> Items(IActionResult r)
    {
        var valor = Assert.IsType<OkObjectResult>(r).Value!;
        return ((IEnumerable<Habitacion>)valor.GetType().GetProperty("items")!.GetValue(valor)!).ToList();
    }

    [Fact]
    public async Task El_listado_incluye_las_imagenes_de_cada_habitacion_en_orden()
    {
        var db = Datos.NuevoDb(); var h1 = Datos.Habitacion(db); var h2 = Datos.Habitacion(db);
        var segunda = Datos.Imagen(db, h1.Id, 1); var primera = Datos.Imagen(db, h1.Id, 0);

        var items = Items(await new HabitacionesController(db).Listar(null, null, null, false));

        var de1 = items.Single(h => h.Id == h1.Id);
        Assert.Equal(new[] { primera.Id, segunda.Id }, de1.Imagenes.Select(i => i.Id));
        Assert.Equal(primera.Url, de1.Imagenes[0].Url);
        Assert.Empty(items.Single(h => h.Id == h2.Id).Imagenes);
    }

    [Fact]
    public async Task El_detalle_incluye_las_imagenes()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db);
        var img = Datos.Imagen(db, h.Id, 0);

        var r = await new HabitacionesController(db).Obtener(h.Id);

        var habitacion = Assert.IsType<Habitacion>(Assert.IsType<OkObjectResult>(r).Value);
        Assert.Equal(img.Id, Assert.Single(habitacion.Imagenes).Id);
    }

    [Fact]
    public async Task El_detalle_de_una_habitacion_sin_imagenes_trae_una_lista_vacia()
    {
        var db = Datos.NuevoDb(); var h = Datos.Habitacion(db);
        var habitacion = Assert.IsType<Habitacion>(Assert.IsType<OkObjectResult>(await new HabitacionesController(db).Obtener(h.Id)).Value);
        Assert.Empty(habitacion.Imagenes);
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH; dotnet test habitaciones-api.Tests 2>&1 | grep -E "error CS|Passed|Failed|×" | head -6`
Expected: las 2 primeras pruebas fallan (`Imagenes` sigue vacía); la tercera pasa. (Compila: `Listar` y `Obtener` ya existen. Los parámetros de `Listar` tienen valores por defecto para `page` y `size`.)

- [ ] **Step 3: Implementar en `HabitacionesController.cs`**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'habitaciones-api/Controllers/HabitacionesController.cs'
s = open(p, encoding='utf-8').read()

old1 = "        return Ok(new { total, page, size, items });"
assert s.count(old1) == 1
s = s.replace(old1, "        await CargarImagenes(items);\n        return Ok(new { total, page, size, items });")

old2 = """        var h = await db.Habitaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return h is null ? NotFound() : Ok(h);"""
assert s.count(old2) == 1
s = s.replace(old2, """        var h = await db.Habitaciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        await CargarImagenes([h]);
        return Ok(h);""")

old3 = "    private async Task<bool> HuespedAlojado"
assert s.count(old3) == 1
s = s.replace(old3, """    // Rellena `Imagenes` (ordenadas) de las habitaciones dadas, con una sola consulta
    private async Task CargarImagenes(IReadOnlyCollection<Habitacion> habitaciones)
    {
        if (habitaciones.Count == 0) return;
        var ids = habitaciones.Select(h => h.Id).ToList();
        var filas = await db.HabitacionImagenes.AsNoTracking().Where(i => ids.Contains(i.HabitacionId))
            .OrderBy(i => i.Orden).ThenBy(i => i.CreadoEn).ToListAsync();
        var porHabitacion = filas.ToLookup(i => i.HabitacionId, i => new ImagenVista(i.Id, i.Url, i.Orden));
        foreach (var h in habitaciones) h.Imagenes = porHabitacion[h.Id].ToList();
    }

""" + old3)
open(p, 'w', encoding='utf-8').write(s)
print('HabitacionesController actualizado')
EOF
```
Expected: `HabitacionesController actualizado`. (Si el ancla `HuespedAlojado` no se encuentra porque la rama base no contiene la validación de estado, usar como ancla `private async Task<bool> TieneReservasVigentes` y repetir.)

- [ ] **Step 4: Ejecutar y comprobar que pasa**

Run: `dotnet build habitaciones-api 2>&1 | grep -E " error |Error\(s\)" | head -3; dotnet test habitaciones-api.Tests 2>&1 | tail -4`
Expected: `0 Error(s)` y `Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add habitaciones-api habitaciones-api.Tests
git commit -m "feat(habitaciones-api): el listado y el detalle incluyen las imágenes de cada habitación"
```

---

### Task 7: `mis-reservas/disponibles` incluye imágenes (reservas-api)

**Files:**
- Modify: `reservas-api/Models/Reserva.cs`
- Modify: `reservas-api/Data/ReservasDbContext.cs`
- Modify: `reservas-api/Controllers/MisReservasController.cs`

**Interfaces:**
- Consumes: tabla `public.habitacion_imagenes` (Task 1).
- Produces: cada elemento de `GET /api/mis-reservas/disponibles` trae `imagenes: [{ url }]` ordenadas. `reservas-front` lo usa en la Task 10. Esta API no tiene proyecto de pruebas: se verifica compilando y en la Task 11.

- [ ] **Step 1: Agregar el modelo de solo lectura**

En `reservas-api/Models/Reserva.cs`, justo después de la clase `HabitacionRef` (termina con `[Column("activo")] public bool Activo { get; set; }\n}`), agregar:

```csharp
[Table("habitacion_imagenes", Schema = "public")]
public class HabitacionImagenRef
{
    [Column("id")] public Guid Id { get; set; }
    [Column("habitacion_id")] public Guid HabitacionId { get; set; }
    [Column("url")] public string Url { get; set; } = "";
    [Column("orden")] public int Orden { get; set; }
    [Column("creado_en")] public DateTime CreadoEn { get; set; }
}
```

En `reservas-api/Data/ReservasDbContext.cs`, agregar dentro de la clase, tras `DbSet<HabitacionRef> Habitaciones`:

```csharp
    public DbSet<HabitacionImagenRef> HabitacionImagenes => Set<HabitacionImagenRef>();
```

- [ ] **Step 2: Incluir las imágenes en `Disponibles`**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'reservas-api/Controllers/MisReservasController.cs'
s = open(p, encoding='utf-8').read()
old = "        return Ok(libres);"
assert s.count(old) == 1
s = s.replace(old, """        var ids = libres.Select(h => h.Id).ToList();
        var filas = await db.HabitacionImagenes.AsNoTracking().Where(i => ids.Contains(i.HabitacionId))
            .OrderBy(i => i.Orden).ThenBy(i => i.CreadoEn).ToListAsync();
        var urls = filas.ToLookup(i => i.HabitacionId, i => new { i.Url });
        return Ok(libres.Select(h => new
        {
            h.Id, h.Numero, h.Tipo, h.Capacidad, h.PrecioNoche, h.noches, h.total,
            imagenes = urls[h.Id].ToList(),
        }));""")
open(p, 'w', encoding='utf-8').write(s)
print('MisReservasController actualizado')
EOF
```
Expected: `MisReservasController actualizado`.

- [ ] **Step 3: Compilar**

Run: `export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH; dotnet build reservas-api 2>&1 | grep -E " error |Error\(s\)|Warn" | head -5`
Expected: `0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add reservas-api
git commit -m "feat(reservas-api): las habitaciones disponibles incluyen sus imágenes"
```

---

### Task 8: Imagen de relleno `SIN_FOTO` en `hotel-ui` (TDD)

**Files:**
- Create: `hotel-ui/src/assets.js`
- Create: `hotel-ui/src/assets.test.js`
- Modify: `hotel-ui/src/index.js`, `hotel-ui/src/index.test.js`

**Interfaces:**
- Produces: `SIN_FOTO` (string, URL `data:image/svg+xml;...`) exportado desde `hotel-ui`; se pasa como `fallback` a `Carousel` en las Tasks 9 y 10.

- [ ] **Step 1: Escribir la prueba que falla, `hotel-ui/src/assets.test.js`**

```js
import { SIN_FOTO } from './assets.js'

test('SIN_FOTO es una imagen SVG incrustada con el texto "Sin foto"', () => {
  expect(SIN_FOTO.startsWith('data:image/svg+xml')).toBe(true)
  expect(decodeURIComponent(SIN_FOTO)).toContain('Sin foto')
})
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `cd hotel-ui && npx vitest run src/assets.test.js 2>&1 | grep -E "Failed to resolve|FAIL|Test Files"; cd ..`
Expected: falla por `Failed to resolve import "./assets.js"`.

- [ ] **Step 3: Implementar `hotel-ui/src/assets.js`**

```js
// Imagen de relleno para habitaciones sin fotos (SVG incrustado, sin archivos externos)
const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 300"><rect width="400" height="300" fill="#E8DFD0"/><g fill="none" stroke="#6B7280" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"><path d="M110 190h180M120 190v-50h160v50M135 140v-18a14 14 0 0 1 14-14h38a14 14 0 0 1 14 14v18M213 140v-18a14 14 0 0 1 14-14h24a14 14 0 0 1 14 14v18"/></g><text x="200" y="245" text-anchor="middle" font-family="sans-serif" font-size="22" fill="#6B7280">Sin foto</text></svg>`

export const SIN_FOTO = `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`
```

- [ ] **Step 4: Exportar y ampliar la prueba de exports**

En `hotel-ui/src/index.js`, agregar al final:

```js
export { SIN_FOTO } from './assets.js'
```

En `hotel-ui/src/index.test.js`, agregar `'SIN_FOTO'` a la lista `PUBLICOS` (después de `'ImageManager',`).

- [ ] **Step 5: Ejecutar toda la suite**

Run: `cd hotel-ui && npm test 2>&1 | grep -E "Test Files|Tests "; cd ..`
Expected: todo pasa (64 pruebas: las 63 de antes más 1).

- [ ] **Step 6: Documentar en el README de `hotel-ui`**

Agregar al final de la sección `### ImageManager` del `hotel-ui/README.md` (antes de `## Hooks`):

```markdown
### SIN_FOTO
Imagen de relleno (SVG incrustado) para habitaciones sin fotos. Se pasa como `fallback` de `Carousel`:
`<Carousel images={h.imagenes} fallback={SIN_FOTO} />`.

```

- [ ] **Step 7: Commit**

```bash
git add hotel-ui
git commit -m "feat(hotel-ui): imagen de relleno SIN_FOTO para carruseles sin imágenes"
```

---

### Task 9: Carrusel en el listado y gestor de imágenes en el formulario (`habitaciones-front`)

**Files:**
- Modify: `habitaciones-front/src/api.js`
- Modify: `habitaciones-front/src/pages/HabitacionesList.jsx`
- Modify: `habitaciones-front/src/pages/HabitacionForm.jsx` (reemplazo completo)
- Modify: `habitaciones-front/src/styles.css`

**Interfaces:**
- Consumes: `Carousel`, `ImageManager`, `SIN_FOTO`, `ConfirmModal` de `hotel-ui`; la API de las Tasks 4 a 6 (`imagenes` en las lecturas; `POST/DELETE/PUT .../imagenes`).
- Produces: `habitacionesApi.subirImagen(id, archivo)`, `habitacionesApi.borrarImagen(id, imagenId)`, `habitacionesApi.reordenarImagenes(id, ids)`.

- [ ] **Step 1: `api.js`: soportar `FormData` y agregar las 3 llamadas**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'habitaciones-front/src/api.js'
s = open(p, encoding='utf-8').read()

old = "    headers: { 'Content-Type': 'application/json', ...auth() },"
assert s.count(old) == 1
s = s.replace(old, "    // con FormData el navegador pone el Content-Type (multipart + boundary)\n    headers: { ...(options.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }), ...auth() },")

old2 = "  desactivar: (id) => req(`/api/habitaciones/${id}`, { method: 'DELETE' }),\n"
assert s.count(old2) == 1
s = s.replace(old2, old2 + """  subirImagen: (id, archivo) => {
    const datos = new FormData()
    datos.append('archivo', archivo)
    return req(`/api/habitaciones/${id}/imagenes`, { method: 'POST', body: datos })
  },
  borrarImagen: (id, imagenId) => req(`/api/habitaciones/${id}/imagenes/${imagenId}`, { method: 'DELETE' }),
  reordenarImagenes: (id, ids) => req(`/api/habitaciones/${id}/imagenes/orden`, { method: 'PUT', body: JSON.stringify({ ids }) }),
""")
open(p, 'w', encoding='utf-8').write(s)
print('api.js actualizado')
EOF
```
Expected: `api.js actualizado`.

- [ ] **Step 2: Carrusel en el listado, `HabitacionesList.jsx`**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'habitaciones-front/src/pages/HabitacionesList.jsx'
s = open(p, encoding='utf-8').read()

old = "import { Alert, Button, Card, Checkbox, ConfirmModal, DataTable, Input, Pager, Select, Toolbar, useDebouncedEffect } from 'hotel-ui'"
assert s.count(old) == 1
s = s.replace(old, "import { Alert, Button, Card, Carousel, Checkbox, ConfirmModal, DataTable, Input, Pager, SIN_FOTO, Select, Toolbar, useDebouncedEffect } from 'hotel-ui'")

old2 = "  const columns = [\n    { header: 'N.º', cell: (h) => <strong>{h.numero}</strong> },"
assert s.count(old2) == 1
s = s.replace(old2, """  const columns = [
    { header: '', cell: (h) => (
      <div className="thumb"><Carousel images={h.imagenes} alt={`Habitación ${h.numero}`} fallback={SIN_FOTO} /></div>
    ) },
    { header: 'N.º', cell: (h) => <strong>{h.numero}</strong> },""")
open(p, 'w', encoding='utf-8').write(s)
print('HabitacionesList actualizado')
EOF
```
Expected: `HabitacionesList actualizado`.

- [ ] **Step 3: Reemplazar `habitaciones-front/src/pages/HabitacionForm.jsx` completo**

```jsx
import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { validar, requerido, patron, entre, maxLen, Alert, Button, Card, Checkbox, ConfirmModal, Field, FormActions, FormGrid, ImageManager, Input, Select } from 'hotel-ui'
import { habitacionesApi, ESTADOS, TIPOS } from '../api.js'

const vacio = { numero: '', tipo: 'Sencilla', piso: 1, capacidad: 1, precioNoche: 0, estado: 'Disponible', descripcion: '', activo: true }

const ESQUEMA = {
  numero: [requerido(), patron(/^[A-Za-z0-9-]{1,10}$/, 'Solo letras, números y guion (máx. 10)')],
  piso: [requerido(), entre(0, 50, 'El piso debe estar entre 0 y 50')],
  capacidad: [requerido(), entre(1, 10, 'La capacidad debe estar entre 1 y 10')],
  precioNoche: [requerido(), entre(1000, 50000000, 'El precio debe estar entre $1.000 y $50.000.000')],
  descripcion: [maxLen(500)],
}

export default function HabitacionForm() {
  const { id } = useParams()
  const nav = useNavigate()
  const [f, setF] = useState(vacio)
  const [imagenes, setImagenes] = useState([])
  const [aBorrar, setABorrar] = useState(null)
  const [error, setError] = useState('')
  const [errorImagenes, setErrorImagenes] = useState('')
  const [saving, setSaving] = useState(false)
  const [intento, setIntento] = useState(false)
  const errs = intento ? validar(f, ESQUEMA) : {}

  useEffect(() => {
    if (!id) { setImagenes([]); return setF(vacio) }
    habitacionesApi.obtener(id)
      .then(({ imagenes: imgs = [], ...h }) => {
        setImagenes(imgs)
        setF({ ...vacio, ...Object.fromEntries(Object.entries(h).map(([k, v]) => [k, v ?? ''])) })
      })
      .catch((e) => setError(e.message))
  }, [id])

  const set = (k) => (e) => setF({ ...f, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value })

  // Solo recarga las imágenes: no pisa lo que se esté editando en el formulario
  const refrescarImagenes = async () => setImagenes((await habitacionesApi.obtener(id)).imagenes ?? [])
  const subir = async (archivo) => { setErrorImagenes(''); await habitacionesApi.subirImagen(id, archivo); await refrescarImagenes() }
  const reordenar = async (ids) => { setErrorImagenes(''); await habitacionesApi.reordenarImagenes(id, ids); await refrescarImagenes() }
  const confirmarBorrado = async () => {
    const imagenId = aBorrar
    setABorrar(null); setErrorImagenes('')
    try { await habitacionesApi.borrarImagen(id, imagenId); await refrescarImagenes() } catch (err) { setErrorImagenes(err.message) }
  }

  const guardar = async (e) => {
    e.preventDefault()
    setIntento(true)
    if (Object.keys(validar(f, ESQUEMA)).length) return
    setSaving(true); setError('')
    const body = {
      ...f, piso: Number(f.piso), capacidad: Number(f.capacidad), precioNoche: Number(f.precioNoche),
      descripcion: f.descripcion || null,
    }
    delete body.id; delete body.creadoEn
    try {
      id ? await habitacionesApi.actualizar(id, body) : await habitacionesApi.crear(body)
      nav('/habitaciones')
    } catch (err) { setError(err.message) } finally { setSaving(false) }
  }

  return (
    <>
      <Card className="form">
        <form onSubmit={guardar} noValidate>
          <h2>{id ? 'Editar habitación' : 'Nueva habitación'}</h2>
          <Alert>{error}</Alert>
          <FormGrid>
            <Field label="Número" error={errs.numero}><Input required maxLength="10" value={f.numero} onChange={set('numero')} /></Field>
            <Field label="Tipo"><Select value={f.tipo} onChange={set('tipo')} options={TIPOS} /></Field>
            <Field label="Piso" error={errs.piso}><Input type="number" min="0" required value={f.piso} onChange={set('piso')} /></Field>
            <Field label="Capacidad (personas)" error={errs.capacidad}><Input type="number" min="1" required value={f.capacidad} onChange={set('capacidad')} /></Field>
            <Field label="Precio por noche (COP)" error={errs.precioNoche}><Input type="number" min="0" step="1000" required value={f.precioNoche} onChange={set('precioNoche')} /></Field>
            <Field label="Estado"><Select value={f.estado} onChange={set('estado')} options={ESTADOS} /></Field>
            <Field label="Descripción" full error={errs.descripcion}><Input maxLength="500" value={f.descripcion} onChange={set('descripcion')} /></Field>
            <Checkbox full label="Habitación activa" checked={f.activo} onChange={set('activo')} />
          </FormGrid>
          <FormActions>
            <Button type="button" variant="ghost" onClick={() => nav('/habitaciones')}>Cancelar</Button>
            <Button disabled={saving}>{saving ? 'Guardando…' : 'Guardar'}</Button>
          </FormActions>
        </form>
      </Card>

      <Card className="form">
        <h2>Imágenes</h2>
        {id ? (
          <>
            <Alert>{errorImagenes}</Alert>
            <ImageManager images={imagenes} onUpload={subir} onDelete={setABorrar} onReorder={reordenar} />
          </>
        ) : (
          <p>Guarda la habitación para poder agregar imágenes.</p>
        )}
      </Card>

      <ConfirmModal open={!!aBorrar} danger title="Eliminar imagen" confirmText="Eliminar"
        message="¿Eliminar esta imagen de la habitación?" onConfirm={confirmarBorrado} onCancel={() => setABorrar(null)} />
    </>
  )
}
```

- [ ] **Step 4: Estilos en `habitaciones-front/src/styles.css`**

Agregar al final:

```css
.thumb{width:120px}
.thumb .carousel-btn{width:24px;height:24px;font-size:1rem}
.thumb .carousel-dots{bottom:4px}
.form + .form{margin-top:16px}
```

- [ ] **Step 5: Compilar**

Run: `cd habitaciones-front && npx vite build 2>&1 | tail -3; cd ..; grep -rn "<button\|<input\|<select" habitaciones-front/src; echo "(fin)"`
Expected: `✓ built in ...` y ningún elemento crudo.

- [ ] **Step 6: Commit**

```bash
git add habitaciones-front
git commit -m "feat(habitaciones-front): carrusel en el listado y gestor de imágenes en el formulario"
```

---

### Task 10: Carrusel en el portal del huésped (`reservas-front`)

**Files:**
- Modify: `reservas-front/src/pages/Reservar.jsx`
- Modify: `reservas-front/src/styles.css`

**Interfaces:**
- Consumes: `Carousel`, `SIN_FOTO` de `hotel-ui`; `imagenes: [{ url }]` de `GET /api/mis-reservas/disponibles` (Task 7).

- [ ] **Step 1: Mostrar el carrusel en cada habitación disponible**

Ejecutar:
```bash
python3 - <<'EOF'
p = 'reservas-front/src/pages/Reservar.jsx'
s = open(p, encoding='utf-8').read()

old = "import { validar, requerido, entre, hoyLocal, Alert, Button, Card, ConfirmModal, Field, FormActions, FormGrid, Input, Spinner } from 'hotel-ui'"
assert s.count(old) == 1
s = s.replace(old, "import { validar, requerido, entre, hoyLocal, Alert, Button, Card, Carousel, ConfirmModal, Field, FormActions, FormGrid, Input, SIN_FOTO, Spinner } from 'hotel-ui'")

old2 = "            <Card key={h.id} className=\"room\">\n              <h3>"
assert s.count(old2) == 1
s = s.replace(old2, "            <Card key={h.id} className=\"room\">\n              <Carousel images={h.imagenes} alt={`Habitación ${h.numero}`} fallback={SIN_FOTO} />\n              <h3>")
open(p, 'w', encoding='utf-8').write(s)

c = 'reservas-front/src/styles.css'
t = open(c, encoding='utf-8').read()
open(c, 'w', encoding='utf-8').write(t.rstrip('\n') + "\n.room .carousel{margin-bottom:12px}\n")
print('Reservar actualizado')
EOF
```
Expected: `Reservar actualizado`.

- [ ] **Step 2: Compilar**

Run: `cd reservas-front && npx vite build 2>&1 | tail -2; cd ..; grep -rn "<button\|<input\|<select" reservas-front/src; echo "(fin)"`
Expected: `✓ built in ...` y ningún elemento crudo.

- [ ] **Step 3: Commit**

```bash
git add reservas-front
git commit -m "feat(reservas-front): carrusel de imágenes en las habitaciones disponibles"
```

---

### Task 11: Verificación final de extremo a extremo (y limpieza de datos de prueba)

**Files:** ninguno (verificación). Los hallazgos que obliguen a cambiar código se corrigen en la tarea correspondiente con su prueba.

**Interfaces:**
- Consumes: todo lo anterior y la tabla real `habitacion_imagenes` (Task 1, Step 4).

- [ ] **Step 1: Suites y compilaciones**

Run:
```bash
export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH
dotnet test habitaciones-api.Tests 2>&1 | tail -3
dotnet build reservas-api 2>&1 | grep -E "Error\(s\)"
npm --prefix hotel-ui test 2>&1 | grep -E "Test Files|Tests "
for d in auth-front clientes-front habitaciones-front reservas-front; do printf "%-20s" $d; (cd $d && npx vite build 2>&1 | tail -1); done
git status --short
```
Expected: `Failed: 0`, `0 Error(s)`, todas las pruebas de `hotel-ui` pasan, los 4 fronts compilan y el árbol está limpio.

- [ ] **Step 2: Confirmar que la tabla existe**

Repetir el Step 3 de la Task 1. Expected: `habitacion_imagenes`. Si dice `NO EXISTE`, detenerse y pedir al usuario que ejecute `db/habitacion_imagenes.sql` (ver Task 1, Step 4).

- [ ] **Step 3: Levantar los servicios**

Run:
```bash
export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec; export PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH
p=5001; for d in auth-api clientes-api habitaciones-api reservas-api; do (cd $d && nohup dotnet run --urls http://localhost:$p > "$TMPDIR/$d.log" 2>&1 &); p=$((p+1)); done
for d in auth-front clientes-front habitaciones-front reservas-front; do (cd $d && nohup npm run dev > "$TMPDIR/$d.log" 2>&1 &); done
sleep 25; for p in 5001 5002 5003 5004 5173 5174 5175 5176; do echo "$p -> $(curl -s -o /dev/null -w '%{http_code}' --max-time 5 http://localhost:$p/)"; done
```
Expected: los 8 responden (404 en las APIs, 200 en los fronts). Si `habitaciones-api` no arranca, leer `$TMPDIR/habitaciones-api.log`: un `InvalidOperationException` de `SUPABASE_URL`/`SUPABASE_SECRET_KEY` indica que el `.env` de `habitaciones-api` no las tiene.

- [ ] **Step 4: Probar el API con archivos reales (curl)**

Crear los archivos de prueba y definir el preámbulo (sin imprimir el token):
```bash
python3 - <<'EOF'
import os, struct, zlib
d = os.environ.get('TMPDIR', '/tmp').rstrip('/')
def png(path, rgb):
    raw = b''.join(b'\x00' + bytes(rgb) * 40 for _ in range(30))
    def chunk(t, data): return struct.pack('>I', len(data)) + t + data + struct.pack('>I', zlib.crc32(t + data) & 0xffffffff)
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 40, 30, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b''))
colores = [(200,60,60),(60,160,90),(60,90,200),(220,180,50),(150,60,180),(60,180,180),(120,120,120)]
for i, c in enumerate(colores): png(f'{d}/hab-prueba-{i}.png', c)
open(f'{d}/falso.png', 'w').write('<html><script>alert(1)</script></html>')
open(f'{d}/vacio.png', 'wb').close()
open(f'{d}/grande.png', 'wb').write(open(f'{d}/hab-prueba-0.png', 'rb').read() + b'\0' * (5 * 1024 * 1024 + 10))
print('archivos listos en', d)
EOF
# --- Preámbulo: repetirlo al inicio de cada llamada de shell de los Steps 5 y 8 (las variables no persisten entre llamadas)
TOK=$(curl -s --max-time 40 -X POST http://localhost:5001/api/auth/login -H 'Content-Type: application/json' -d '{"email":"admin@hotel.com","password":"Admin123*"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
HID=$(curl -s -H "Authorization: Bearer $TOK" "http://localhost:5003/api/habitaciones?q=301" | sed -E 's/.*"items":\[\{"id":"([^"]+)".*/\1/'); echo "habitación 301: $HID"
B=http://localhost:5003/api/habitaciones/$HID/imagenes
subir(){ curl -s -w " [%{http_code}]\n" -H "Authorization: Bearer $TOK" -F "archivo=@$TMPDIR/$1;type=${2:-image/png}" $B; }
# --- fin del preámbulo
echo "1) válida:"; subir hab-prueba-0.png
echo "2) falso (HTML con extensión png):"; subir falso.png
echo "3) vacío:"; subir vacio.png
echo "4) mayor de 5 MB:"; subir grande.png
echo "5) sin token:"; curl -s -o /dev/null -w "[%{http_code}]\n" -F "archivo=@$TMPDIR/hab-prueba-1.png" $B
```
Expected: 1) `201` con `{"id":...,"url":"https://...supabase.co/storage/v1/object/public/habitaciones/<id>/<guid>.png","orden":0}`; 2) `400` «Formato no permitido…»; 3) `400` «La imagen está vacía.»; 4) `400` «La imagen supera 5 MB.» (o `413` si el servidor corta antes por tamaño: aceptable, anotarlo); 5) `401`.

- [ ] **Step 5: Más pruebas: límite de 6, lectura, orden, borrado y bucket**

Run (empezar con el preámbulo del Step 4):
```bash
# [pegar aquí el preámbulo del Step 4]
for i in 1 2 3 4 5; do subir hab-prueba-$i.png | tail -c 8; done
echo "7ª imagen:"; subir hab-prueba-6.png
echo "lectura (imagenes del detalle):"; curl -s -H "Authorization: Bearer $TOK" http://localhost:5003/api/habitaciones/$HID | sed -E 's/.*"imagenes":(\[[^]]*\]).*/\1/' | head -c 400; echo
DETALLE=$(curl -s -H "Authorization: Bearer $TOK" http://localhost:5003/api/habitaciones/$HID)
PRIMERA=$(echo "$DETALLE" | python3 -c "import sys,json; print(json.load(sys.stdin)['imagenes'][0]['id'])")
ORD=$(echo "$DETALLE" | python3 -c "import sys,json; ids=[i['id'] for i in json.load(sys.stdin)['imagenes']]; print(json.dumps({'ids': ids[::-1]}))")
URL1=$(curl -s -H "Authorization: Bearer $TOK" http://localhost:5003/api/habitaciones/$HID | python3 -c "import sys,json; print(json.load(sys.stdin)['imagenes'][0]['url'])")
echo "URL pública de la 1ª imagen -> $(curl -s -o /dev/null -w '%{http_code}' "$URL1")"
echo "reordenar válido (invertir):"; curl -s -o /dev/null -w "[%{http_code}]\n" -X PUT -H "Authorization: Bearer $TOK" -H 'Content-Type: application/json' -d "$ORD" $B/orden
echo "reordenar con lista incompleta:"; curl -s -w " [%{http_code}]\n" -X PUT -H "Authorization: Bearer $TOK" -H 'Content-Type: application/json' -d "{\"ids\":[\"$PRIMERA\"]}" $B/orden
echo "borrar una imagen inexistente:"; curl -s -o /dev/null -w "[%{http_code}]\n" -X DELETE -H "Authorization: Bearer $TOK" $B/00000000-0000-0000-0000-000000000000
```
Expected: las 5 subidas devuelven `201`; la 7ª → `409` «Máximo 6 imágenes por habitación.»; la lectura muestra 6 objetos con `url`; la URL pública responde `200`; reordenar válido `200`; lista incompleta `400`; borrar inexistente `404`.

- [ ] **Step 6: Verificar el carrusel y el gestor en el navegador integrado**

1. Abrir `http://localhost:5173/login`, entrar con `admin@hotel.com` (contraseña de prueba del README) y abrir Habitaciones.
2. En el listado, la habitación 301 muestra un carrusel de 6 imágenes (flechas y puntos) y las demás muestran la imagen «Sin foto». Probar las flechas.
3. Abrir «Editar» de la 301: aparece la tarjeta «Imágenes» con 6 miniaturas, contador «6 de 6 imágenes» y «Agregar imagen» deshabilitado. Pulsar «←»/«→» de una miniatura y comprobar que el orden cambia; pulsar «Eliminar» de una: aparece el diálogo de confirmación; confirmar y ver «5 de 6 imágenes».
4. Abrir «Nueva habitación»: la tarjeta «Imágenes» dice «Guarda la habitación para poder agregar imágenes.»
5. Revisar la consola del navegador: sin errores.

- [ ] **Step 7: Verificar el portal del huésped (cuenta de prueba, con limpieza)**

Registrar una cuenta de huésped de prueba por la API (crea 1 fila en `clientes` y 1 en `usuarios`) y consultar las habitaciones disponibles:
```bash
curl -s -o /dev/null -w "registro: [%{http_code}]\n" -X POST http://localhost:5001/api/auth/registro -H 'Content-Type: application/json' -d '{"tipoDocumento":"CC","numeroDocumento":"9990000001","nombres":"Prueba","apellidos":"Imagenes","email":"prueba-imagenes@example.com","telefono":null,"password":"Prueba123*"}'
GT=$(curl -s -X POST http://localhost:5001/api/auth/login -H 'Content-Type: application/json' -d '{"email":"prueba-imagenes@example.com","password":"Prueba123*"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
MA=$(date -v+30d +%Y-%m-%d); PA=$(date -v+31d +%Y-%m-%d)
curl -s -H "Authorization: Bearer $GT" "http://localhost:5004/api/mis-reservas/disponibles?entrada=$MA&salida=$PA&huespedes=1" | python3 -c "import sys,json; [print(h['numero'], len(h['imagenes']), 'imágenes') for h in json.load(sys.stdin)]"
```
Expected: `registro: [200]` y una línea por habitación disponible con su número de imágenes (la 301 con 5, las demás con 0).
Luego, en el navegador integrado: cerrar sesión, entrar como `prueba-imagenes@example.com` (contraseña `Prueba123*`), ir a «Reservar» (reservas), buscar disponibilidad para fechas dentro de 30 días y comprobar que la tarjeta de la 301 muestra el carrusel y las demás «Sin foto». No confirmar ninguna reserva.

- [ ] **Step 8: Limpieza obligatoria de los datos de prueba**

Apagar los servicios y borrar todo lo creado (empezar con el preámbulo del Step 4):
```bash
# [pegar aquí el preámbulo del Step 4]
# 1) Borrar las imágenes de prueba (elimina fila y objeto del bucket)
for ID in $(curl -s -H "Authorization: Bearer $TOK" http://localhost:5003/api/habitaciones/$HID | python3 -c "import sys,json; print(' '.join(i['id'] for i in json.load(sys.stdin)['imagenes']))"); do curl -s -o /dev/null -w "borrar $ID -> %{http_code}\n" -X DELETE -H "Authorization: Bearer $TOK" $B/$ID; done
curl -s -H "Authorization: Bearer $TOK" http://localhost:5003/api/habitaciones/$HID | python3 -c "import sys,json; print('imágenes restantes:', len(json.load(sys.stdin)['imagenes']))"
# 2) Borrar la cuenta de huésped de prueba (usuario primero, luego la ficha de cliente)
cd habitaciones-api && set -a && . ./.env && set +a && export PGPASSWORD="$SUPABASE_PASSWORD" PGCONNECT_TIMEOUT=20 && psql -h "$SUPABASE_HOST" -p "$SUPABASE_PORT" -U "$SUPABASE_USER" -d "$SUPABASE_DB" -tA -c "delete from public.usuarios where email='prueba-imagenes@example.com'" -c "delete from public.clientes where email='prueba-imagenes@example.com' and numero_documento='9990000001'" -c "select count(*) from public.usuarios where email='prueba-imagenes@example.com'" -c "select count(*) from public.habitacion_imagenes"; cd ..
# 3) Apagar los servicios
pkill -f "dotnet run --urls"; pkill -f vite
for p in 5001 5002 5003 5004 5173 5174 5175 5176; do lsof -tiTCP:$p -sTCP:LISTEN | xargs kill 2>/dev/null; done; sleep 3
echo "escuchando: $(for p in 5001 5002 5003 5004 5173 5174 5175 5176; do lsof -tiTCP:$p -sTCP:LISTEN; done | wc -l | tr -d ' ')"
git status --short
```
Expected: cada imagen se borra con `204`; `imágenes restantes: 0`; los dos `delete` confirman `DELETE 1` y los dos `select count(*)` devuelven `0`; `escuchando: 0` y árbol limpio. Los PNG de prueba están en `$TMPDIR` (fuera del repositorio).

- [ ] **Step 9: Informar al usuario**

Resumen de lo verificado, commits de `feat/imagenes-habitaciones-impl`, que no se hizo `push`, y la lista de datos de prueba creados y borrados (imágenes de la 301, cuenta `prueba-imagenes@example.com`).
