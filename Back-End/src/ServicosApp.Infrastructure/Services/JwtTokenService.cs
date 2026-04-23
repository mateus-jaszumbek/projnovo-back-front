using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ServicosApp.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) GerarToken(
        Usuario usuario,
        Guid? empresaId,
        string? empresaNomeFantasia,
        string? perfil,
        int nivelAcesso)
    {
        var jwtSection = _configuration.GetSection("Jwt");

        var key = jwtSection["Key"]
                  ?? throw new InvalidOperationException("Jwt:Key não configurado.");

        var issuer = jwtSection["Issuer"]
                     ?? throw new InvalidOperationException("Jwt:Issuer não configurado.");

        var audience = jwtSection["Audience"]
                       ?? throw new InvalidOperationException("Jwt:Audience não configurado.");

        var expireMinutes = int.TryParse(jwtSection["ExpireMinutes"], out var minutes)
            ? minutes
            : 120;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new("isSuperAdmin", usuario.IsSuperAdmin ? "true" : "false")
        };

        if (usuario.IsSuperAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "superadmin"));
        }

        if (empresaId.HasValue)
        {
            claims.Add(new Claim("empresaId", empresaId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(empresaNomeFantasia))
        {
            claims.Add(new Claim("empresaNomeFantasia", empresaNomeFantasia));
        }

        if (!string.IsNullOrWhiteSpace(perfil))
        {
            claims.Add(new Claim("perfil", perfil));
            claims.Add(new Claim(ClaimTypes.Role, perfil));
        }

        claims.Add(new Claim("nivelAcesso", nivelAcesso.ToString()));

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expireMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenValue, expiresAtUtc);
    }
}
