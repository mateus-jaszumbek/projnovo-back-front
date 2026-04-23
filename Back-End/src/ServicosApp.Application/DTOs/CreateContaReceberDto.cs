using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateContaReceberDto
{
    public Guid? ClienteId { get; set; }

    [MaxLength(30)]
    public string? OrigemTipo { get; set; }

    public Guid? OrigemId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    public DateOnly? DataEmissao { get; set; }

    [Required]
    public DateOnly DataVencimento { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Valor { get; set; }

    [MaxLength(30)]
    public string? FormaPagamento { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}