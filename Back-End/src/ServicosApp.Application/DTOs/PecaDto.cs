namespace ServicosApp.Application.DTOs;

public class PecaDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

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
    public string? FornecedorNome { get; set; }
    public string? FornecedorWhatsApp { get; set; }
    public string? FornecedorEmail { get; set; }
    public string? FornecedorMensagemPadrao { get; set; }

    public decimal CustoUnitario { get; set; }
    public decimal PrecoVenda { get; set; }
    public int GarantiaDias { get; set; }
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; }

    public bool Ativo { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
