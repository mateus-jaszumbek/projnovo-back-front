namespace ServicosApp.Application.DTOs;

public class RegraFiscalProdutoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string TipoDocumentoFiscal { get; set; } = string.Empty;
    public string? UfOrigem { get; set; }
    public string? UfDestino { get; set; }
    public string? RegimeTributario { get; set; }
    public string? Ncm { get; set; }

    public string Cfop { get; set; } = string.Empty;
    public string CstCsosn { get; set; } = string.Empty;
    public string? Cest { get; set; }
    public string OrigemMercadoria { get; set; } = string.Empty;

    public decimal AliquotaIcms { get; set; }
    public decimal AliquotaPis { get; set; }
    public decimal AliquotaCofins { get; set; }

    public bool Ativo { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
