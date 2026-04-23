namespace ServicosApp.Application.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }

    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public bool IsSuperAdmin { get; set; }

    public Guid? EmpresaId { get; set; }
    public string? EmpresaNomeFantasia { get; set; }
    public string? Perfil { get; set; }
    public int NivelAcesso { get; set; }
}
