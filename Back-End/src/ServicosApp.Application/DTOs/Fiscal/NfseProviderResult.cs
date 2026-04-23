namespace ServicosApp.Application.DTOs.Fiscal;

public class NfseProviderResult
{
    public bool Sucesso { get; set; }

    public string? Status { get; set; }
    public string? NumeroExterno { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? Protocolo { get; set; }
    public string? CodigoVerificacao { get; set; }
    public string? LinkConsulta { get; set; }
    public string? Lote { get; set; }

    public string? XmlConteudo { get; set; }
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }

    public string? CodigoErro { get; set; }
    public string? MensagemErro { get; set; }

    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
}