using HabitacionesApi.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HabitacionesApi.Tests;

public class StorageTests
{
    // Simula el vencimiento del Timeout de HttpClient (que lanza TaskCanceledException, no HttpRequestException)
    private sealed class ManejadorQueExpira : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout");
    }

    private static SupabaseStorage ConManejador(HttpMessageHandler m) =>
        new(new HttpClient(m), new StorageOptions("https://x.supabase.co", "k", "habitaciones"), NullLogger<SupabaseStorage>.Instance);

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

    // Review: un timeout de Storage es un fallo de Storage, no un error inesperado
    [Fact]
    public async Task Un_timeout_al_subir_es_un_fallo_de_Storage()
    {
        var storage = ConManejador(new ManejadorQueExpira());
        await Assert.ThrowsAsync<ImagenStorageException>(() => storage.SubirAsync("a/b.png", new MemoryStream([1]), "image/png", default));
    }

    [Fact]
    public async Task Un_timeout_al_borrar_es_un_fallo_de_Storage()
    {
        var storage = ConManejador(new ManejadorQueExpira());
        await Assert.ThrowsAsync<ImagenStorageException>(() => storage.BorrarAsync("a/b.png", default));
    }

    [Fact]
    public async Task La_cancelacion_pedida_por_el_llamador_no_se_confunde_con_un_timeout()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var storage = ConManejador(new ManejadorQueExpira());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.SubirAsync("a/b.png", new MemoryStream([1]), "image/png", cts.Token));
    }
}
