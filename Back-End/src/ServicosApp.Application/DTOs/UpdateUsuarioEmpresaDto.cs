namespace ServicosApp.Application.DTOs;

public class UpdateUsuarioEmpresaDto
{
    public string Perfil { get; set; } = "atendente";
    public int NivelAcesso { get; set; } = 2;
    public bool Ativo { get; set; } = true;
}
