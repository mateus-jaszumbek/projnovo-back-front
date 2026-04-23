namespace ServicosApp.Application.DTOs;

public class ServicoCatalogoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal ValorPadrao { get; set; }
    public string? CodigoInterno { get; set; }
    public int? TempoEstimadoMinutos { get; set; }
    public int GarantiaDias { get; set; }
    public bool Ativo { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}