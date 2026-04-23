namespace ServicosApp.Domain.Entities;

public class ContaPagar
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public Guid? FornecedorId { get; set; }
    public Fornecedor? FornecedorCadastro { get; set; }
    public string? Fornecedor { get; set; }
    public string? Categoria { get; set; }

    public DateOnly DataEmissao { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly DataVencimento { get; set; }

    public decimal Valor { get; set; }
    public decimal ValorPago { get; set; }

    public string Status { get; set; } = "PENDENTE";
    public string? Observacoes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
