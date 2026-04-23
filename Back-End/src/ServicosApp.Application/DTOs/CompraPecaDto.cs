using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CompraPecaDto
{
    [Required]
    public Guid PecaId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantidade { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CustoUnitario { get; set; }

    [MaxLength(200)]
    public string? Fornecedor { get; set; }
    public Guid? FornecedorId { get; set; }

    [Required]
    public DateOnly DataVencimento { get; set; }

    public bool GerarContaPagar { get; set; } = true;

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}
