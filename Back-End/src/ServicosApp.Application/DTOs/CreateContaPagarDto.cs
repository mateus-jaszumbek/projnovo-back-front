using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateContaPagarDto
{
    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Fornecedor { get; set; }
    public Guid? FornecedorId { get; set; }

    [MaxLength(100)]
    public string? Categoria { get; set; }

    public DateOnly? DataEmissao { get; set; }

    [Required]
    public DateOnly DataVencimento { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Valor { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}
