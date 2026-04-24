namespace ServicosApp.Application.DTOs;

public class FiscalReadinessDto
{
    public Guid EmpresaId { get; set; }
    public string Ambiente { get; set; } = "Homologacao";
    public string? ProviderCode { get; set; }
    public bool FocusProviderSelected { get; set; }
    public bool HomologacaoReady { get; set; }
    public bool ProducaoReady { get; set; }
    public int OkCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> MissingForHomologacao { get; set; } = [];
    public List<string> MissingForProducao { get; set; } = [];
    public List<string> NextSteps { get; set; } = [];
    public List<FiscalChecklistItemDto> Items { get; set; } = [];
}
