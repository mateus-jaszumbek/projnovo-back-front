using System.ComponentModel.DataAnnotations;

public class CreateEmpresaDto
{
    [Required(ErrorMessage = "Razão social é obrigatória.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Razão social deve ter entre 3 e 150 caracteres.")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome fantasia é obrigatório.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Nome fantasia deve ter entre 2 e 150 caracteres.")]
    public string NomeFantasia { get; set; } = string.Empty;

    [Required(ErrorMessage = "CNPJ é obrigatório.")]
    [RegularExpression(@"^\d{14}$", ErrorMessage = "CNPJ deve conter 14 dígitos numéricos.")]
    public string Cnpj { get; set; } = string.Empty;

    [StringLength(20)]
    public string? InscricaoEstadual { get; set; }

    [StringLength(20)]
    public string? InscricaoMunicipal { get; set; }

    [StringLength(30)]
    public string? RegimeTributario { get; set; }

    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Telefone inválido.")]
    public string? Telefone { get; set; }

    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? LogoUrl { get; set; }
}