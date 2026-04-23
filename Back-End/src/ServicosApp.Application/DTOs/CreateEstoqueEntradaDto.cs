using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateEstoqueEntradaDto
{
    [Required]
    public Guid PecaId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantidade { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CustoUnitario { get; set; }

    [MaxLength(500)]
    public string? Observacao { get; set; }
}