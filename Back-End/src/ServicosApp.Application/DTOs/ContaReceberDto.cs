namespace ServicosApp.Application.DTOs;

public class ContaReceberDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public Guid? ClienteId { get; set; }
    public string? ClienteNome { get; set; }

    public string? OrigemTipo { get; set; }
    public Guid? OrigemId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public DateOnly DataEmissao { get; set; }
    public DateOnly DataVencimento { get; set; }

    public decimal Valor { get; set; }
    public decimal ValorRecebido { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? FormaPagamento { get; set; }
    public string? Observacoes { get; set; }

    public DateTime CreatedAt { get; set; }
}