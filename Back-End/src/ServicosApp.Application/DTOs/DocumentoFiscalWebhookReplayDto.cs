namespace ServicosApp.Application.DTOs;

public class DocumentoFiscalWebhookReplayDto
{
    public Guid DocumentoFiscalId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public string? NumeroExterno { get; set; }
    public string? StatusAtual { get; set; }
    public bool ReenvioAceito { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}
