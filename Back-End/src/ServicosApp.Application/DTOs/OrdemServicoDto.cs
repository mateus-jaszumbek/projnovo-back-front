namespace ServicosApp.Application.DTOs;

public class OrdemServicoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public long NumeroOs { get; set; }

    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;

    public Guid AparelhoId { get; set; }
    public string AparelhoDescricao { get; set; } = string.Empty;

    public Guid? TecnicoId { get; set; }
    public string? TecnicoNome { get; set; }

    public string Status { get; set; } = string.Empty;

    public string DefeitoRelatado { get; set; } = string.Empty;
    public string? Diagnostico { get; set; }
    public string? LaudoTecnico { get; set; }
    public string? ObservacoesInternas { get; set; }
    public string? ObservacoesCliente { get; set; }
    public string? EmpresaLogoUrl { get; set; }
    public List<OrdemServicoFotoDto> Fotos { get; set; } = new();

    public decimal ValorMaoObra { get; set; }
    public decimal ValorPecas { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }

    public DateTime DataEntrada { get; set; }
    public DateTime? DataPrevisao { get; set; }
    public int GarantiaDias { get; set; }
    public DateTime? DataAprovacao { get; set; }
    public DateTime? DataConclusao { get; set; }
    public DateTime? DataEntrega { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
