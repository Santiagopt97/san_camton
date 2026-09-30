using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthApi.Data;
using AuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthDbContext db, IConfiguration cfg) : ControllerBase
{
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == dto.Email.Trim().ToLower());
        if (u is null || !u.Activo || !BCrypt.Net.BCrypt.Verify(dto.Password, u.PasswordHash))
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });

        return Ok(Emitir(u));
    }

    // Registro público: crea la cuenta de un huésped y su ficha de cliente
    [EnableRateLimiting("auth")]
    [HttpPost("registro")]
    public async Task<IActionResult> Registro(RegistroDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var doc = dto.NumeroDocumento.Trim();
        if (await db.Usuarios.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "Ya existe una cuenta con ese correo." });

        if (await db.Clientes.AnyAsync(c => c.Email != null && c.Email.ToLower() == email && c.NumeroDocumento != doc))
            return Conflict(new { message = "Ese correo ya está asociado a otro cliente." });

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.NumeroDocumento == doc);
        if (cliente is not null)
        {
            // Ya lo registró recepción: solo se enlaza si el correo coincide con el de la ficha
            if (!string.Equals(cliente.Email, email, StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = "Ese documento ya está registrado. Pide a recepción que asocie tu correo o usa el mismo correo de tu ficha." });
            if (await db.Usuarios.AnyAsync(x => x.ClienteId == cliente.Id))
                return Conflict(new { message = "Ese documento ya tiene una cuenta." });
        }
        else
        {
            cliente = new ClienteReg
            {
                Id = Guid.NewGuid(), TipoDocumento = dto.TipoDocumento, NumeroDocumento = doc,
                Nombres = dto.Nombres.Trim(), Apellidos = dto.Apellidos.Trim(), Email = email,
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(), CreadoEn = DateTime.UtcNow,
            };
            db.Clientes.Add(cliente);
        }
        var u = new Usuario
        {
            Id = Guid.NewGuid(), Email = email, Nombre = $"{cliente.Nombres} {cliente.Apellidos}".Trim(),
            Perfil = "huesped", ClienteId = cliente.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        };
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return Ok(Emitir(u));
    }

    private object Emitir(Usuario u)
    {
        var jwt = cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var expira = DateTime.UtcNow.AddMinutes(jwt.GetValue("ExpiraMinutos", 120));
        var claims = new List<Claim>
        {
            new("sub", u.Id.ToString()), new("name", u.Nombre), new("email", u.Email), new("role", u.Perfil),
        };
        if (u.ClienteId is not null) claims.Add(new Claim("cliente_id", u.ClienteId.ToString()!));
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims,
            expires: expira, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expira,
            usuario = new { u.Id, u.Nombre, u.Email, u.Perfil },
        };
    }

    [Authorize, HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        nombre = User.FindFirstValue("name"),
        email = User.FindFirstValue("email"),
        perfil = User.FindFirstValue("role"),
    });
}
