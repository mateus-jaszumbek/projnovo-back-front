using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateVendaItemDto
{
    public string TipoItem { get; set; } = "PECA";

    public Guid? PecaId { get; set; }

    public Guid? ServicoCatalogoId { get; set; }

    public string? Descricao { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantidade { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? ValorUnitario { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Desconto { get; set; }
}