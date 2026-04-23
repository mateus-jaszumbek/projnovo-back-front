namespace ServicosApp.Application.DTOs;

public class CaixaLancamentoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public Guid CaixaDiarioId { get; set; }

    public string Tipo { get; set; } = string.Empty;
    public string? OrigemTipo { get; set; }
    public Guid? OrigemId { get; set; }

    public string? FormaPagamento { get; set; }
    public decimal Valor { get; set; }

    public string? Observacao { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}