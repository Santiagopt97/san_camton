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
