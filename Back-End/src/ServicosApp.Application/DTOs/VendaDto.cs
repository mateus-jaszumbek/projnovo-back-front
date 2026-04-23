namespace ServicosApp.Application.DTOs;

public class VendaDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public long NumeroVenda { get; set; }

    public Guid? ClienteId { get; set; }
    public string? ClienteNome { get; set; }

    public string Status { get; set; } = string.Empty;
    public string FormaPagamento { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }

    public string? Observacoes { get; set; }
    public DateTime DataVenda { get; set; }
    public bool Ativo { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}