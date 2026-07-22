namespace ServicosApp.Application.DTOs.Pagamentos;

public class PagamentoProviderResult
{
    public bool Sucesso { get; set; }
    public string? ExternalId { get; set; }
    public string? Status { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? QrCodePayload { get; set; }
    public string? MensagemErro { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
}
