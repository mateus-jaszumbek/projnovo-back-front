namespace ServicosApp.Application.DTOs;

public class FocusWebhookProcessResultDto
{
    public bool Enabled { get; set; }
    public bool Processed { get; set; }
    public string? IgnoredReason { get; set; }
    public Guid? DocumentoFiscalId { get; set; }
    public Guid? EmpresaId { get; set; }
    public string? TipoDocumento { get; set; }
    public string? Reference { get; set; }
    public string? ProviderStatus { get; set; }
    public string? StatusBefore { get; set; }
    public string? StatusAfter { get; set; }
}
