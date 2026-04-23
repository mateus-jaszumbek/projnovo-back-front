using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class UpdateOrdemServicoDto
{
    [Required]
    public Guid ClienteId { get; set; }

    [Required]
    public Guid AparelhoId { get; set; }

    public Guid? TecnicoId { get; set; }

    [Required]
    public string DefeitoRelatado { get; set; } = string.Empty;

    public string? Diagnostico { get; set; }
    public string? LaudoTecnico { get; set; }
    public string? ObservacoesInternas { get; set; }
    public string? ObservacoesCliente { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ValorMaoObra { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Desconto { get; set; }
    public DateTime? DataPrevisao { get; set; }

    [Range(0, int.MaxValue)]
    public int GarantiaDias { get; set; }

    public Guid? UpdatedBy { get; set; }
}