namespace ServicosApp.Application.DTOs;

public class DocumentoFiscalDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string TipoDocumento { get; set; } = string.Empty;
    public string OrigemTipo { get; set; } = string.Empty;
    public Guid OrigemId { get; set; }

    public long Numero { get; set; }
    public int Serie { get; set; }

    public string? SerieRps { get; set; }
    public long? NumeroRps { get; set; }

    public string Status { get; set; } = string.Empty;
    public string Ambiente { get; set; } = string.Empty;

    public Guid? ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string? ClienteCpfCnpj { get; set; }

    public DateTime DataEmissao { get; set; }
    public DateTime? DataCompetencia { get; set; }
    public DateTime? DataAutorizacao { get; set; }
    public DateTime? DataCancelamento { get; set; }

    public string? ChaveAcesso { get; set; }
    public string? Protocolo { get; set; }
    public string? CodigoVerificacao { get; set; }
    public string? LinkConsulta { get; set; }
    public string? NumeroExterno { get; set; }
    public string? Lote { get; set; }

    public decimal ValorServicos { get; set; }
    public decimal ValorProdutos { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }
    public bool GerarContaReceberQuandoAutorizar { get; set; }

    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }

    public string? CodigoRejeicao { get; set; }
    public string? MensagemRejeicao { get; set; }
    public string? MotivoCancelamento { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
