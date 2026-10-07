namespace HotelSecurity.Tests;

// Guardia de regresión para las 4 APIs: solo habitaciones-api tiene pruebas con la API arrancada, así que aquí se comprueba
// que ninguna pierda el cableado de la sesión por cookie (lectura del token, CORS con credenciales y protección CSRF en su orden).
public class ProgramWiringTests
{
    private static string Raiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !(Directory.Exists(Path.Combine(d.FullName, "hotel-security")) && Directory.Exists(Path.Combine(d.FullName, "auth-api"))))
            d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio");
    }

    [Theory]
    [InlineData("auth-api")]
    [InlineData("clientes-api")]
    [InlineData("habitaciones-api")]
    [InlineData("reservas-api")]
    public void La_API_lee_la_cookie_acepta_credenciales_y_aplica_CSRF_antes_de_autenticar(string api)
    {
        var texto = File.ReadAllText(Path.Combine(Raiz(), api, "Program.cs"));

        Assert.Contains("OnMessageReceived = TokenDesdeCookie.Leer", texto);
        Assert.Contains(".AllowCredentials()", texto);

        var cors = texto.IndexOf("app.UseCors();", StringComparison.Ordinal);
        var csrf = texto.IndexOf("app.UseCsrfHeader();", StringComparison.Ordinal);
        var auth = texto.IndexOf("app.UseAuthentication();", StringComparison.Ordinal);
        Assert.True(cors >= 0, $"{api}: falta app.UseCors()");
        Assert.True(csrf > cors, $"{api}: app.UseCsrfHeader() debe ir después de app.UseCors()");
        Assert.True(auth > csrf, $"{api}: app.UseCsrfHeader() debe ir antes de app.UseAuthentication()");
    }

    // La landing (puerto 5177) llama a habitaciones-api desde otro origen: si falta CORS_ORIGINS en el .env, debe funcionar igual
    [Fact]
    public void Habitaciones_acepta_por_defecto_el_origen_de_la_landing()
    {
        var texto = File.ReadAllText(Path.Combine(Raiz(), "habitaciones-api", "Program.cs"));
        Assert.Contains("?? \"http://localhost:5174,http://localhost:5177\"", texto);
    }
}
