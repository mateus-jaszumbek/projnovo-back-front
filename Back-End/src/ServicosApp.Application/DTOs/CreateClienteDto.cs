namespace ServicosApp.Application.DTOs;

public class CreateClienteDto
{
    public Guid EmpresaId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string TipoPessoa { get; set; } = "FISICA";
    public string? CpfCnpj { get; set; }

    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public DateOnly? DataAniversario { get; set; }

    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }

    public string? Observacoes { get; set; }
    public bool EhLojista { get; set; }

    public bool IndicadoPorTerceiro { get; set; }
    public string? NomeIndicacao { get; set; }
}