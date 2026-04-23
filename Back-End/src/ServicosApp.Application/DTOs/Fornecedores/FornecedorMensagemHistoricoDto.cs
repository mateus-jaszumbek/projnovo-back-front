namespace ServicosApp.Application.DTOs.Fornecedores;

public class FornecedorMensagemHistoricoDto
{
    public Guid Id { get; set; }
    public Guid FornecedorId { get; set; }
    public string FornecedorNome { get; set; } = string.Empty;
    public Guid? PecaId { get; set; }
    public string? PecaNome { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public decimal? QuantidadeSolicitada { get; set; }
    public DateTime EnviadoEm { get; set; }
    public DateTime CreatedAt { get; set; }
}
