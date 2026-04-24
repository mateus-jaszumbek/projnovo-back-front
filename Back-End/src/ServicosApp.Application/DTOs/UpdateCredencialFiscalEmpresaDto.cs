using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class UpdateCredencialFiscalEmpresaDto
{
    [Required]
    [MaxLength(20)]
    public string TipoDocumentoFiscal { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Provedor { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? UrlBase { get; set; }

    [MaxLength(200)]
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }
    public bool LimparClientSecret { get; set; }

    [MaxLength(200)]
    public string? UsuarioApi { get; set; }

    public string? SenhaApi { get; set; }
    public bool LimparSenhaApi { get; set; }

    public string? CertificadoBase64 { get; set; }
    public string? CertificadoSenha { get; set; }
    public bool LimparCertificado { get; set; }

    public string? TokenAcesso { get; set; }
    public DateTime? TokenExpiraEm { get; set; }
    public bool LimparTokenAcesso { get; set; }

    public bool Ativo { get; set; } = true;
}
