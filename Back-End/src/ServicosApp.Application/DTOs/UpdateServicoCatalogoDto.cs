using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class UpdateServicoCatalogoDto
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Valor padrão deve ser maior ou igual a zero.")]
    public decimal ValorPadrao { get; set; }

    [MaxLength(50)]
    public string? CodigoInterno { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Tempo estimado deve ser maior que zero.")]
    public int? TempoEstimadoMinutos { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Garantia deve ser maior ou igual a zero.")]
    public int GarantiaDias { get; set; } = 0;

    public bool Ativo { get; set; } = true;
}