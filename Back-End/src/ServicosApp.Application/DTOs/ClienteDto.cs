namespace ServicosApp.Application.DTOs;

public class ClienteDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string TipoPessoa { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }

    public string? Telefone { get; set; }
    public string? Email { get; set; }

    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }

    public string? Observacoes { get; set; }
    public bool Ativo { get; set; }
}