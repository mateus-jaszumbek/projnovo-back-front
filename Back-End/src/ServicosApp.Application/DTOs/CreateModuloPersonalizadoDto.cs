using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateModuloPersonalizadoDto
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descricao { get; set; }

    public int Ordem { get; set; }
}
