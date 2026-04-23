using ServicosApp.Domain.Entities;

namespace ServicosApp.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GerarToken(
        Usuario usuario,
        Guid? empresaId,
        string? empresaNomeFantasia,
        string? perfil,
        int nivelAcesso);
}
