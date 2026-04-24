using ServicosApp.Domain.Enums;

namespace ServicosApp.Domain.Entities;

public class DocumentoFiscal : EmpresaOwnedEntity
{
    public TipoDocumentoFiscal TipoDocumento { get; set; } = TipoDocumentoFiscal.Nfse;
    public OrigemDocumentoFiscal OrigemTipo { get; set; } = OrigemDocumentoFiscal.OrdemServico;
    public Guid OrigemId { get; set; }

    public long Numero { get; set; }
    public int Serie { get; set; }

    public string? SerieRps { get; set; }
    public long? NumeroRps { get; set; }

    public StatusDocumentoFiscal Status { get; set; } = StatusDocumentoFiscal.Rascunho;
    public AmbienteFiscal Ambiente { get; set; } = AmbienteFiscal.Homologacao;

    public Guid? ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string? ClienteCpfCnpj { get; set; }
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefone { get; set; }

    public string? ClienteCep { get; set; }
    public string? ClienteLogradouro { get; set; }
    public string? ClienteNumero { get; set; }
    public string? ClienteComplemento { get; set; }
    public string? ClienteBairro { get; set; }
    public string? ClienteCidade { get; set; }
    public string? ClienteUf { get; set; }
    public string? ClienteMunicipioCodigo { get; set; }

    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;
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

    public string? XmlConteudo { get; set; }
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }

    public string? CodigoRejeicao { get; set; }
    public string? MensagemRejeicao { get; set; }

    public string? PayloadEnvio { get; set; }
    public string? PayloadRetorno { get; set; }

    public string? MotivoCancelamento { get; set; }

    public Guid? CreatedBy { get; set; }
    public Usuario? UsuarioCriacao { get; set; }

    public List<DocumentoFiscalItem> Itens { get; set; } = new();
    public List<EventoFiscal> Eventos { get; set; } = new();
}
