namespace ServicosApp.Domain.Entities;

public class Peca : EmpresaOwnedEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? CodigoInterno { get; set; }
    public string? Sku { get; set; }
    public string? Descricao { get; set; } 
    public string? Categoria { get; set; }
    public string? Marca { get; set; }
    public string? ModeloCompativel { get; set; }
    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string? CfopPadraoNfe { get; set; }
    public string? CfopPadraoNfce { get; set; }
    public string? CstCsosn { get; set; }
    public string? OrigemMercadoria { get; set; }
    public string Unidade { get; set; } = "UN";
    public Guid? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    public decimal CustoUnitario { get; set; }
    public decimal PrecoVenda { get; set; }
    public int GarantiaDias { get; set; } = 0;
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; }

    public bool Ativo { get; set; } = true;

    public List<EstoqueMovimento> MovimentosEstoque { get; set; } = new();
    public List<VendaItem> ItensVenda { get; set; } = new();
    public List<OrdemServicoItem> ItensOrdemServico { get; set; } = new();
}
