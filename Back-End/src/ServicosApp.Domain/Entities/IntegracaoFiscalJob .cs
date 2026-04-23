namespace ServicosApp.Domain.Entities;

public class IntegracaoFiscalJob : EmpresaOwnedEntity
{
    public Guid DocumentoFiscalId { get; set; }

    public string TipoOperacao { get; set; } = string.Empty; // EMITIR, CONSULTAR, CANCELAR
    public string Status { get; set; } = "PENDENTE";

    public int Tentativas { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public string? MensagemErro { get; set; }
    public DocumentoFiscal? DocumentoFiscal { get; set; }

    public DateTime? ProcessadoEm { get; set; }
}