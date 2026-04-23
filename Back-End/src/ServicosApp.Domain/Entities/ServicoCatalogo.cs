namespace ServicosApp.Domain.Entities;

public class ServicoCatalogo : EmpresaOwnedEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal ValorPadrao { get; set; }
    public string? CodigoInterno { get; set; }
    public int? TempoEstimadoMinutos { get; set; }
    public int GarantiaDias { get; set; } = 0;
    public bool Ativo { get; set; } = true;

    public List<OrdemServicoItem> ItensOrdemServico { get; set; } = new();
}