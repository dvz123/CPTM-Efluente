using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Cptm.Api.Services;

public class JwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly int _expiracaoMinutos;

    public JwtService(IConfiguration config)
    {
        _secretKey = config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey não configurada.");
        _issuer = config["Jwt:Issuer"] ?? "CptmAmbiental";
        _expiracaoMinutos = int.TryParse(config["Jwt:ExpiracaoMinutos"], out var minutos) ? minutos : 15;

    }

    public (string token, DateTime expiraEm) GerarToken(int userId, string email, string perfil, string nome = "")
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_expiracaoMinutos);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Role, perfil),
            new Claim("perfil", perfil)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: expiraEm,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
