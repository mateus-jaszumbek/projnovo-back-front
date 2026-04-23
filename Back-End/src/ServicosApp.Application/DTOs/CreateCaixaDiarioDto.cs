using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateCaixaDiarioDto
{
    [Required]
    public DateOnly DataCaixa { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ValorAbertura { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}