namespace ServicosApp.Application.DTOs;

public class OrdemServicoImeiHistoricoDto
{
    public Guid OrdemServicoId { get; set; }
    public long NumeroOs { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string AparelhoDescricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DefeitoRelatado { get; set; } = string.Empty;
    public DateTime DataEntrada { get; set; }
    public DateTime? DataEntrega { get; set; }
    public int GarantiaDias { get; set; }
    public DateTime? DataVencimentoGarantia { get; set; }
    public string SituacaoGarantia { get; set; } = "SEM_GARANTIA";
}
