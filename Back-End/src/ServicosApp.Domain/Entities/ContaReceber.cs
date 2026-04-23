namespace ServicosApp.Domain.Entities;

public class ContaReceber
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string? OrigemTipo { get; set; }
    public Guid? OrigemId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public DateOnly DataEmissao { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly DataVencimento { get; set; }

    public decimal Valor { get; set; }
    public decimal ValorRecebido { get; set; }

    public string Status { get; set; } = "PENDENTE";
    public string? FormaPagamento { get; set; }
    public string? Observacoes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}