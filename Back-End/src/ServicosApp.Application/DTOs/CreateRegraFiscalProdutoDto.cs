using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateRegraFiscalProdutoDto
{
    [Required]
    [MaxLength(20)]
    public string TipoDocumentoFiscal { get; set; } = "Nfce";

    [MaxLength(2)]
    public string? UfOrigem { get; set; }

    [MaxLength(2)]
    public string? UfDestino { get; set; }

    [MaxLength(50)]
    public string? RegimeTributario { get; set; }

    [MaxLength(20)]
    public string? Ncm { get; set; }

    [Required]
    [MaxLength(10)]
    public string Cfop { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CstCsosn { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Cest { get; set; }

    [Required]
    [MaxLength(2)]
    public string OrigemMercadoria { get; set; } = "0";

    [Range(0, 100)]
    public decimal AliquotaIcms { get; set; }

    [Range(0, 100)]
    public decimal AliquotaPis { get; set; }

    [Range(0, 100)]
    public decimal AliquotaCofins { get; set; }

    public bool Ativo { get; set; } = true;

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}
