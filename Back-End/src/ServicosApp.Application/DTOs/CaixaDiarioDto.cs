namespace ServicosApp.Application.DTOs;

public class CaixaDiarioDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public DateOnly DataCaixa { get; set; }

    public decimal ValorAbertura { get; set; }
    public decimal ValorFechamentoSistema { get; set; }
    public decimal? ValorFechamentoInformado { get; set; }
    public decimal? Diferenca { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid? AbertoPor { get; set; }
    public Guid? FechadoPor { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }

    public string? Observacoes { get; set; }
}