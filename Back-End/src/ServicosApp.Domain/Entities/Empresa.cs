namespace ServicosApp.Domain.Entities;

public class Empresa : EntityBase
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;

    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string RegimeTributario { get; set; } = "SimplesNacional";

    public string? Email { get; set; }
    public string? Telefone { get; set; }

    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }

    public string? LogoUrl { get; set; }
    public bool Ativo { get; set; } = true;

    public List<UsuarioEmpresa> UsuarioEmpresas { get; set; } = new();
    public List<Cliente> Clientes { get; set; } = new();
    public List<Aparelho> Aparelhos { get; set; } = new();
}