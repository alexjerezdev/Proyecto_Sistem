using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CafeAroma.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace CafeAroma.Api.Services;

public class TokenService
{
    private readonly IConfiguration _config;
    public TokenService(IConfiguration config) => _config = config;

    public string GenerarToken(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim("usuario_id", usuario.UsuarioId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}