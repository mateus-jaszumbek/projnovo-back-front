namespace ServicosApp.Application.DTOs.Pagamentos;

public class CobrancaPagamentoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string Canal { get; set; } = string.Empty;

    public string OrigemTipo { get; set; } = string.Empty;
    public Guid OrigemId { get; set; }

    public decimal Valor { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public string? QrCodeBase64 { get; set; }
    public string? QrCodePayload { get; set; }
    public string? MensagemErro { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? PagoEm { get; set; }
}

public class CreateCobrancaPagamentoDto
{
    public string OrigemTipo { get; set; } = string.Empty;
    public Guid OrigemId { get; set; }
    public decimal Valor { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string? Descricao { get; set; }
}
