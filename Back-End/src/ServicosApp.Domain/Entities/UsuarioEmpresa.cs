namespace ServicosApp.Domain.Entities;

public class UsuarioEmpresa : EntityBase
{
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public Guid EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }

    public string Perfil { get; set; } = "owner";
    public int NivelAcesso { get; set; } = 5;
    public bool Ativo { get; set; } = true;
}
