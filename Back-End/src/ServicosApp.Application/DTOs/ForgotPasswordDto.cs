using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;
}
