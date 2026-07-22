namespace ServicosApp.Domain.Entities;

public class CobrancaPagamento : EmpresaOwnedEntity
{
    public string Provider { get; set; } = string.Empty;
    public string Canal { get; set; } = string.Empty;

    public string OrigemTipo { get; set; } = string.Empty;
    public Guid OrigemId { get; set; }

    public decimal Valor { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public string? ExternalId { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? QrCodePayload { get; set; }
    public string? MensagemErro { get; set; }

    public DateTime? PagoEm { get; set; }
    public Guid? CreatedBy { get; set; }
}
