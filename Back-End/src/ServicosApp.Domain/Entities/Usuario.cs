namespace ServicosApp.Domain.Entities;

public class Usuario : EntityBase
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string? TermosUsoVersaoAceita { get; set; }
    public DateTime? TermosUsoAceitoEmUtc { get; set; }
    public string? PoliticaPrivacidadeVersaoAceita { get; set; }
    public DateTime? PoliticaPrivacidadeAceitaEmUtc { get; set; }
    public bool Ativo { get; set; } = true;
    public bool IsSuperAdmin { get; set; } = false;

    public List<UsuarioEmpresa> UsuarioEmpresas { get; set; } = new();
}
