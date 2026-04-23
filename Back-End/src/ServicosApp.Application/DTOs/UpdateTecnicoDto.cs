using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class UpdateTecnicoDto
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [EmailAddress(ErrorMessage = "Email inválido.")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Especialidade { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public bool Ativo { get; set; } = true;
}