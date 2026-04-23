namespace ServicosApp.Application.DTOs;

public class CreateUsuarioEmpresaDto
{
    public Guid UsuarioId { get; set; }
    public Guid EmpresaId { get; set; }
    public string Perfil { get; set; } = "owner";
    public int NivelAcesso { get; set; } = 2;
    public bool Ativo { get; set; } = true;
}
