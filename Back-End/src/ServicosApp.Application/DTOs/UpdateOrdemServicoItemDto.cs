using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class UpdateOrdemServicoItemDto
{
    [Required]
    [MaxLength(20)]
    public string TipoItem { get; set; } = string.Empty; // SERVICO ou PECA

    public Guid? ServicoCatalogoId { get; set; }
    public Guid? PecaId { get; set; }

    [MaxLength(200)]
    public string? Descricao { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantidade { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? ValorUnitario { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Desconto { get; set; }
}