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
