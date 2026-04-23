using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class EnsureModuloSistemaDto
{
    [Required]
    [MaxLength(120)]
    public string Chave { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descricao { get; set; }
}
