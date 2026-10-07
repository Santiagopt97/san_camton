using HotelSecurity;

namespace HotelSecurity.Tests;

public class OrigenesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" , ,")]
    public void Sin_valor_util_se_usan_los_origenes_por_defecto(string? valor)
    {
        var porDefecto = new[] { "http://localhost:1" };
        Assert.Equal(porDefecto, Origenes.Parsear(valor, porDefecto));
    }

    [Fact]
    public void Separa_por_comas_recorta_espacios_y_descarta_vacios()
    {
        var r = Origenes.Parsear(" http://a , ,http://b ", ["x"]);
        Assert.Equal(new[] { "http://a", "http://b" }, r);
    }

    // Review: un .env antiguo sin CORS_ORIGINS no debe dejar a los módulos sin poder preguntar quién es el usuario
    [Fact]
    public void Por_defecto_auth_api_acepta_los_cuatro_fronts()
    {
        Assert.Equal(
            new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "http://localhost:5176" },
            Origenes.TodosLosFronts);
    }
}
