using HabitacionesApi.Data;
using HotelSecurity;
using HabitacionesApi.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

DotNetEnv.Env.Load(); // lee el archivo .env (si existe) hacia variables de entorno

var b = WebApplication.CreateBuilder(args);

var pgHost = Environment.GetEnvironmentVariable("SUPABASE_HOST");
var pgPort = Environment.GetEnvironmentVariable("SUPABASE_PORT") ?? "6543";
var pgDb = Environment.GetEnvironmentVariable("SUPABASE_DB") ?? "postgres";
var pgUser = Environment.GetEnvironmentVariable("SUPABASE_USER");
var pgPass = Environment.GetEnvironmentVariable("SUPABASE_PASSWORD");
if (string.IsNullOrWhiteSpace(pgHost) || string.IsNullOrWhiteSpace(pgUser) || string.IsNullOrWhiteSpace(pgPass))
{
    throw new InvalidOperationException(
        "Faltan variables de entorno de Supabase. Copia .env.example como .env y complétalo.");
}
var connStr = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};" +
              "SSL Mode=Require;Trust Server Certificate=true;No Reset On Close=true;Max Auto Prepare=0";

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("Falta JWT_KEY en el .env (debe ser igual a la de auth-api)");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "hotel-auth";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "hotel-apis";
var corsOrigins = (Environment.GetEnvironmentVariable("CORS_ORIGINS") ?? "http://localhost:5174")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
// Orígenes que pueden LEER el endpoint público (la landing): solo GET y sin credenciales
var corsPublico = Origenes.Parsear(Environment.GetEnvironmentVariable("CORS_ORIGINS_PUBLICO"), ["http://localhost:5177"]);

b.Services.AddDbContext<HabitacionesDbContext>(o => o.UseNpgsql(connStr));
// Storage de imágenes: si faltan SUPABASE_URL o SUPABASE_SECRET_KEY, falla al arrancar con un mensaje claro
b.Services.AddSingleton(StorageOptions.Desde(Environment.GetEnvironmentVariable));
b.Services.AddHttpClient<IImagenStorage, SupabaseStorage>(c => c.Timeout = TimeSpan.FromSeconds(30)); // por defecto serían 100 s
b.Services.AddControllers();
b.Services.AddEndpointsApiExplorer();
b.Services.AddSwaggerGen();

b.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.Events = new JwtBearerEvents { OnMessageReceived = TokenDesdeCookie.Leer };
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtIssuer, ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = "role", NameClaimType = "name",
    };
});
b.Services.AddAuthorization();

b.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
    // La landing no necesita sesión: solo lectura pública, sin credenciales
    o.AddPolicy("publico", p => p.WithOrigins(corsPublico).WithMethods("GET").AllowAnyHeader());
});
b.Services.AddMemoryCache();

var app = b.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseExceptionHandler(a => a.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Error").LogError(ex, "Error no controlado");
    var pg = (ex as DbUpdateException)?.InnerException as Npgsql.PostgresException;
    (int status, string msg) = pg?.SqlState switch
    {
        "23505" => (409, "Ya existe un registro con esos datos."),
        "23P01" => (409, "La habitación ya está reservada en esas fechas."),
        "23503" => (409, "El registro está relacionado con otros datos y no se puede modificar."),
        "23514" => (400, "Los datos no cumplen las reglas de la base de datos."),
        _ => (500, "Ocurrió un error inesperado. Intenta de nuevo."),
    };
    ctx.Response.StatusCode = status;
    await ctx.Response.WriteAsJsonAsync(new { message = msg });
}));
app.UseCsrfHeader();
app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "habitaciones-api" }));
app.Run();

// Permite que las pruebas arranquen la API completa (WebApplicationFactory)
public partial class Program { }
