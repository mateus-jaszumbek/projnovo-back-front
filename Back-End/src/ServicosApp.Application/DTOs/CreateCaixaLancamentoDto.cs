using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateCaixaLancamentoDto
{
    [Required]
    public Guid CaixaDiarioId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; } = string.Empty; // ENTRADA / SAIDA

    [MaxLength(30)]
    public string? OrigemTipo { get; set; }

    public Guid? OrigemId { get; set; }

    [MaxLength(30)]
    public string? FormaPagamento { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Valor { get; set; }

    [MaxLength(1000)]
    public string? Observacao { get; set; }
}