using System.Runtime.InteropServices.JavaScript;

using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class UpdateVendaDto
{
    public Guid? ClienteId { get; set; }

    [MaxLength(30)]
    public string FormaPagamento { get; set; } = "DINHEIRO";

    [Range(0, double.MaxValue)]
    public decimal Desconto { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }

    public bool Ativo { get; set; } = true;
}