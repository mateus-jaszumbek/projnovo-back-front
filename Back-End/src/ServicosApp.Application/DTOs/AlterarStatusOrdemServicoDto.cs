using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class AlterarStatusOrdemServicoDto
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;
}