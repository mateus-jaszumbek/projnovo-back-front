namespace ServicosApp.Domain.Entities;

public class FornecedorMensagemHistorico : EmpresaOwnedEntity
{
    public Guid FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    public Guid? PecaId { get; set; }
    public Peca? Peca { get; set; }

    public string Canal { get; set; } = "WHATSAPP";
    public string Assunto { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public decimal? QuantidadeSolicitada { get; set; }
    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
}
