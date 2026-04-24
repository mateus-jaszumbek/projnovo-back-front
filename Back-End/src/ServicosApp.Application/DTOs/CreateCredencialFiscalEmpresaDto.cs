using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateCredencialFiscalEmpresaDto
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

    [MaxLength(200)]
    public string? UsuarioApi { get; set; }

    public string? SenhaApi { get; set; }
    public string? CertificadoBase64 { get; set; }
    public string? CertificadoSenha { get; set; }
    public string? TokenAcesso { get; set; }
    public DateTime? TokenExpiraEm { get; set; }

    public bool Ativo { get; set; } = true;
}
