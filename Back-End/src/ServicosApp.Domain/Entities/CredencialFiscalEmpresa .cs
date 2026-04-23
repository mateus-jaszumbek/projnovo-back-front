using ServicosApp.Domain.Enums;

namespace ServicosApp.Domain.Entities;

public class CredencialFiscalEmpresa : EmpresaOwnedEntity
{

    public TipoDocumentoFiscal TipoDocumentoFiscal { get; set; } = TipoDocumentoFiscal.Nfse;
    public string Provedor { get; set; } = string.Empty;

    public string? UrlBase { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecretEncrypted { get; set; }

    public string? UsuarioApi { get; set; }
    public string? SenhaApiEncrypted { get; set; }

    public string? CertificadoBase64Encrypted { get; set; }
    public string? CertificadoSenhaEncrypted { get; set; }

    public string? TokenAcesso { get; set; }
    public DateTime? TokenExpiraEm { get; set; }

    public bool Ativo { get; set; } = true;
}