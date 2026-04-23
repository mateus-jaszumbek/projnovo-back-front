using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateConsumoOrdemServicoDto
{
    [Required]
    public Guid OrdemServicoId { get; set; }

    [Required]
    public Guid PecaId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantidade { get; set; }

    [MaxLength(500)]
    public string? Observacao { get; set; }
}