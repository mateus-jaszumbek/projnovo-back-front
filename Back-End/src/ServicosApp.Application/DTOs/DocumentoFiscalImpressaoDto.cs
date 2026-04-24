namespace ServicosApp.Application.DTOs;

public class DocumentoFiscalImpressaoDto
{
    public Guid Id { get; set; }

    public string TipoDocumento { get; set; } = string.Empty;
    public long Numero { get; set; }
    public int Serie { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Ambiente { get; set; } = string.Empty;

    public DateTime DataEmissao { get; set; }
    public DateTime? DataAutorizacao { get; set; }

    public string EmpresaRazaoSocial { get; set; } = string.Empty;
    public string EmpresaNomeFantasia { get; set; } = string.Empty;
    public string EmpresaCnpj { get; set; } = string.Empty;
    public string? EmpresaTelefone { get; set; }
    public string? EmpresaEmail { get; set; }
    public string? EmpresaLogoUrl { get; set; }
    public string? EmpresaEnderecoCompleto { get; set; }

    public string ClienteNome { get; set; } = string.Empty;
    public string? ClienteCpfCnpj { get; set; }
    public string? ClienteTelefone { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ClienteEnderecoCompleto { get; set; }

    public string? ChaveAcesso { get; set; }
    public string? Protocolo { get; set; }
    public string? CodigoVerificacao { get; set; }
    public string? OfficialPdfUrl { get; set; }

    public decimal ValorServicos { get; set; }
    public decimal ValorProdutos { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }

    public List<DocumentoFiscalImpressaoItemDto> Itens { get; set; } = new();
}

public class DocumentoFiscalImpressaoItemDto
{
    public string TipoItem { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }
    public string? Ncm { get; set; }
    public string? Cfop { get; set; }
    public string? CstCsosn { get; set; }
    public string? ItemListaServico { get; set; }
}
