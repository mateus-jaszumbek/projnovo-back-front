namespace ServicosApp.Application.DTOs.Pagamentos;

public class ConfiguracaoPagamentoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? PublicKey { get; set; }
    public string? PosId { get; set; }
    public string? UserIdExterno { get; set; }

    public bool AccessTokenConfigurado { get; set; }

    public bool SuportaMaquininha { get; set; }
    public bool SuportaPix { get; set; }

    public bool Ativo { get; set; }

    public string? WebhookUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateConfiguracaoPagamentoDto
{
    public string Provider { get; set; } = string.Empty;

    public string? AccessToken { get; set; }
    public string? PublicKey { get; set; }
    public string? PosId { get; set; }
    public string? UserIdExterno { get; set; }

    public bool SuportaMaquininha { get; set; }
    public bool SuportaPix { get; set; }

    public bool Ativo { get; set; } = true;
}

public class PagamentoProviderInfoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool Implementado { get; set; }
    public bool SuportaMaquininha { get; set; }
    public bool SuportaPix { get; set; }
}
