using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class UpdateMinhaEmpresaDto
{
    [Required(ErrorMessage = "Razao social e obrigatoria.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Razao social deve ter entre 3 e 150 caracteres.")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome fantasia e obrigatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Nome fantasia deve ter entre 2 e 150 caracteres.")]
    public string NomeFantasia { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Inscricao estadual deve ter no maximo 20 caracteres.")]
    public string? InscricaoEstadual { get; set; }

    [StringLength(20, ErrorMessage = "Inscricao municipal deve ter no maximo 20 caracteres.")]
    public string? InscricaoMunicipal { get; set; }

    [Required(ErrorMessage = "Regime tributario e obrigatorio.")]
    [StringLength(30, ErrorMessage = "Regime tributario deve ter no maximo 30 caracteres.")]
    public string RegimeTributario { get; set; } = "SimplesNacional";

    [EmailAddress(ErrorMessage = "E-mail invalido.")]
    [StringLength(150, ErrorMessage = "E-mail deve ter no maximo 150 caracteres.")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Telefone invalido.")]
    [StringLength(20, ErrorMessage = "Telefone deve ter no maximo 20 caracteres.")]
    public string? Telefone { get; set; }

    [RegularExpression(@"^\d{8}$", ErrorMessage = "CEP deve conter 8 digitos numericos.")]
    public string? Cep { get; set; }

    [StringLength(150, ErrorMessage = "Logradouro deve ter no maximo 150 caracteres.")]
    public string? Logradouro { get; set; }

    [StringLength(20, ErrorMessage = "Numero deve ter no maximo 20 caracteres.")]
    public string? Numero { get; set; }

    [StringLength(100, ErrorMessage = "Complemento deve ter no maximo 100 caracteres.")]
    public string? Complemento { get; set; }

    [StringLength(100, ErrorMessage = "Bairro deve ter no maximo 100 caracteres.")]
    public string? Bairro { get; set; }

    [StringLength(100, ErrorMessage = "Cidade deve ter no maximo 100 caracteres.")]
    public string? Cidade { get; set; }

    [RegularExpression(@"^[A-Za-z]{2}$", ErrorMessage = "UF deve ter 2 letras.")]
    public string? Uf { get; set; }
}
