namespace ServicosApp.Application.DTOs.Fornecedores;

public class CreateFornecedorMensagemHistoricoDto
{
    public Guid? PecaId { get; set; }
    public string Canal { get; set; } = "WHATSAPP";
    public string? Assunto { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public decimal? QuantidadeSolicitada { get; set; }
}
