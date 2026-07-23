using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "Token é obrigatório.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória.")]
    public string NovaSenha { get; set; } = string.Empty;
}
