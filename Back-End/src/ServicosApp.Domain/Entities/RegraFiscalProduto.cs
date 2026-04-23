using ServicosApp.Domain.Enums;

namespace ServicosApp.Domain.Entities;

public class RegraFiscalProduto : EmpresaOwnedEntity
{
    public TipoDocumentoFiscal TipoDocumentoFiscal { get; set; } = TipoDocumentoFiscal.Nfce;
    public string? UfOrigem { get; set; }
    public string? UfDestino { get; set; }
    public string? RegimeTributario { get; set; }
    public string? Ncm { get; set; }

    public string Cfop { get; set; } = string.Empty;
    public string CstCsosn { get; set; } = string.Empty;
    public string? Cest { get; set; }
    public string OrigemMercadoria { get; set; } = "0";

    public decimal AliquotaIcms { get; set; }
    public decimal AliquotaPis { get; set; }
    public decimal AliquotaCofins { get; set; }

    public bool Ativo { get; set; } = true;
    public string? Observacoes { get; set; }
}
