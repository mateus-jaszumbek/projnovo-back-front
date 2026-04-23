using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class PagarContaPagarDto
{
    [Range(0.01, double.MaxValue)]
    public decimal ValorPago { get; set; }

    public Guid? CaixaDiarioId { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}
