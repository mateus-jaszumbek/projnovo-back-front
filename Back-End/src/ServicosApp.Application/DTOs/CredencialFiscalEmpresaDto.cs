namespace ServicosApp.Application.DTOs;

public class CredencialFiscalEmpresaDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string TipoDocumentoFiscal { get; set; } = string.Empty;
    public string Provedor { get; set; } = string.Empty;

    public string? UrlBase { get; set; }
    public string? ClientId { get; set; }
    public string? UsuarioApi { get; set; }

    public bool ClientSecretConfigurado { get; set; }
    public bool SenhaApiConfigurada { get; set; }
    public bool CertificadoConfigurado { get; set; }
    public bool CertificadoSenhaConfigurada { get; set; }
    public bool TokenAcessoConfigurado { get; set; }
    public DateTime? TokenExpiraEm { get; set; }

    public bool Ativo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
