using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class UpdateCategoriaPecaDto
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
}
