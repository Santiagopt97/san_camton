using System.Text;
using HotelSecurity;
using AuthApi.Data;
using AuthApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

DotNetEnv.Env.Load(); // lee el archivo .env (si existe) hacia variables de entorno

var b = WebApplication.CreateBuilder(args);

// Cadena de conexión armada desde variables de entorno (nunca hardcodeada)
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
    ?? throw new InvalidOperationException("Falta JWT_KEY en el .env");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "hotel-auth";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "hotel-apis";
var jwtExpira = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRA_MINUTOS"), out var m) ? m : 120;
var corsOrigins = Origenes.Parsear(Environment.GetEnvironmentVariable("CORS_ORIGINS"), Origenes.TodosLosFronts);
b.Services.AddDbContext<AuthDbContext>(o => o.UseNpgsql(connStr));
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

b.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

b.Configuration["Jwt:Key"] = jwtKey;
b.Configuration["Jwt:Issuer"] = jwtIssuer;
b.Configuration["Jwt:Audience"] = jwtAudience;
b.Configuration["Jwt:ExpiraMinutos"] = jwtExpira.ToString();

b.Services.AddRateLimiter(o =>
{
    o.OnRejected = async (c, ct) =>
    {
        c.HttpContext.Response.StatusCode = 429;
        await c.HttpContext.Response.WriteAsJsonAsync(new { message = "Demasiados intentos. Espera un minuto e intenta de nuevo." }, ct);
    };
    o.AddPolicy("auth", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = b.Build();

// Usuarios de prueba si la tabla está vacía
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        if (!await db.Usuarios.AnyAsync())
        {
            db.Usuarios.AddRange(
                new Usuario { Id = Guid.NewGuid(), Email = "admin@hotel.com", Nombre = "Administrador", Perfil = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*") },
                new Usuario { Id = Guid.NewGuid(), Email = "recepcion@hotel.com", Nombre = "Recepción", Perfil = "recepcion",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recep123*") });
            await db.SaveChangesAsync();
        }
    }
    catch (Exception e) { app.Logger.LogWarning(e, "No se pudo sembrar usuarios (¿ejecutaste db/usuarios.sql?)"); }
}

app.UseSwagger(); app.UseSwaggerUI();
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
app.UseRateLimiter();
app.UseCsrfHeader();
app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "auth-api" }));
app.Run();
