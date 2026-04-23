namespace ServicosApp.Application.DTOs;

public class ContaPagarDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public Guid? FornecedorId { get; set; }
    public string? Fornecedor { get; set; }
    public string? Categoria { get; set; }

    public DateOnly DataEmissao { get; set; }
    public DateOnly DataVencimento { get; set; }

    public decimal Valor { get; set; }
    public decimal ValorPago { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? Observacoes { get; set; }

    public DateTime CreatedAt { get; set; }
}
