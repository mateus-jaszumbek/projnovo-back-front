using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class FecharCaixaDiarioDto
{
    [Range(0, double.MaxValue)]
    public decimal ValorFechamentoInformado { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }
}