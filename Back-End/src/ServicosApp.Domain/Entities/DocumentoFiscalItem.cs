using ServicosApp.Domain.Enums;

namespace ServicosApp.Domain.Entities;

public class DocumentoFiscalItem : EmpresaOwnedEntity
{
    public Guid DocumentoFiscalId { get; set; }
    public DocumentoFiscal? DocumentoFiscal { get; set; }

    public TipoItemFiscal TipoItem { get; set; } = TipoItemFiscal.Servico;

    public Guid? ServicoCatalogoId { get; set; }
    public ServicoCatalogo? ServicoCatalogo { get; set; }

    public Guid? PecaId { get; set; }
    public Peca? Peca { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }

    public string? Ncm { get; set; }
    public string? Cnae { get; set; }
    public string? ItemListaServico { get; set; }
    public string? Cfop { get; set; }
    public string? Cest { get; set; }
    public string? CstCsosn { get; set; }
    public string? OrigemMercadoria { get; set; }

    public decimal? BaseIss { get; set; }
    public decimal? AliquotaIss { get; set; }
    public decimal? ValorIss { get; set; }
    public bool IssRetido { get; set; }

    public decimal? BaseIcms { get; set; }
    public decimal? AliquotaIcms { get; set; }
    public decimal? ValorIcms { get; set; }

    public decimal? BasePis { get; set; }
    public decimal? AliquotaPis { get; set; }
    public decimal? ValorPis { get; set; }

    public decimal? BaseCofins { get; set; }
    public decimal? AliquotaCofins { get; set; }
    public decimal? ValorCofins { get; set; }
}
