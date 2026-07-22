namespace ServicosApp.Application.DTOs;

public class ClientePortalDto
{
    public string ClienteNome { get; set; } = string.Empty;
    public bool EhLojista { get; set; }
    public string? EmpresaLogoUrl { get; set; }
    public List<ClientePortalOrdemServicoDto> OrdensServico { get; set; } = new();
}

public class ClientePortalOrdemServicoDto
{
    public long NumeroOs { get; set; }
    public string AparelhoDescricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DefeitoRelatado { get; set; } = string.Empty;
    public DateTime DataEntrada { get; set; }
    public DateTime? DataPrevisao { get; set; }
    public DateTime? DataEntrega { get; set; }
    public decimal ValorTotal { get; set; }
    public int GarantiaDias { get; set; }
    public DateTime? DataVencimentoGarantia { get; set; }
    public string SituacaoGarantia { get; set; } = "SEM_GARANTIA";
}
