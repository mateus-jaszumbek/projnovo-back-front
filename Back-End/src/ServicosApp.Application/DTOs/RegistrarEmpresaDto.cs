using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class RegistrarEmpresaDto
{
    [Required(ErrorMessage = "Razão social é obrigatória.")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome fantasia é obrigatório.")]
    public string NomeFantasia { get; set; } = string.Empty;

    [Required(ErrorMessage = "CNPJ é obrigatório.")]
    public string Cnpj { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "E-mail da empresa inválido.")]
    public string? EmailEmpresa { get; set; }

    public string? TelefoneEmpresa { get; set; }

    [Required(ErrorMessage = "Nome do usuário é obrigatório.")]
    public string NomeUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail do usuário é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail do usuário inválido.")]
    public string EmailUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MinLength(7, ErrorMessage = "A senha deve ter mais de 6 caracteres.")]
    public string Senha { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "É obrigatório aceitar os Termos de Uso.")]
    public bool AceitouTermosUso { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "É obrigatório aceitar a Política de Privacidade e LGPD.")]
    public bool AceitouPoliticaPrivacidade { get; set; }
}
