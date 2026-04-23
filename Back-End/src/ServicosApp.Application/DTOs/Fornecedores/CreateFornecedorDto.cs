namespace ServicosApp.Application.DTOs.Fornecedores;

public class CreateFornecedorDto
{
    public string Nome { get; set; } = string.Empty;
    public string TipoPessoa { get; set; } = "JURIDICA";
    public string? CpfCnpj { get; set; }
    public string? Contato { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? ProdutosFornecidos { get; set; }
    public string? MensagemPadrao { get; set; }
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Observacoes { get; set; }
}
