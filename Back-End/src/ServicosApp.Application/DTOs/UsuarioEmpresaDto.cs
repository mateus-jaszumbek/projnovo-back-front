namespace ServicosApp.Application.DTOs;

public class UsuarioEmpresaDto
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }

    public Guid EmpresaId { get; set; }
    public string? EmpresaNomeFantasia { get; set; }

    public string Perfil { get; set; } = string.Empty;
    public int NivelAcesso { get; set; }
    public bool Ativo { get; set; }
}
