using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class ReceberContaReceberDto
{
    [Range(0.01, double.MaxValue)]
    public decimal ValorRecebido { get; set; }

    [MaxLength(30)]
    public string? FormaPagamento { get; set; }

    public Guid? CaixaDiarioId { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}
