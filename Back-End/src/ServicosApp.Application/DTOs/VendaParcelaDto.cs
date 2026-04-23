using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class VendaParcelaDto
{
    [Required]
    public DateOnly DataVencimento { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Valor { get; set; }

    [MaxLength(30)]
    public string? FormaPagamento { get; set; }

    [Range(0, 100)]
    public decimal TaxaPercentual { get; set; }
}
