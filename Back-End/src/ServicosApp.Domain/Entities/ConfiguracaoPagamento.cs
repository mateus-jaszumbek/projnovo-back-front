namespace ServicosApp.Domain.Entities;

public class ConfiguracaoPagamento : EmpresaOwnedEntity
{
    public string Provider { get; set; } = string.Empty;

    public string? AccessTokenEncrypted { get; set; }
    public string? PublicKey { get; set; }
    public string? PosId { get; set; }
    public string? UserIdExterno { get; set; }

    public string WebhookSecret { get; set; } = string.Empty;

    public bool SuportaMaquininha { get; set; }
    public bool SuportaPix { get; set; }

    public bool Ativo { get; set; } = true;
}
