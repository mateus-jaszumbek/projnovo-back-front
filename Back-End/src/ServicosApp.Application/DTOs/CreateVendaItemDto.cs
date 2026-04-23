using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateVendaItemDto
{
    [Required]
    public Guid PecaId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantidade { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? ValorUnitario { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Desconto { get; set; }
}