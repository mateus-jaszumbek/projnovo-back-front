using ServicosApp.Domain.Enums;

namespace ServicosApp.Domain.Entities;

public class EventoFiscal : EmpresaOwnedEntity
{
    public Guid DocumentoFiscalId { get; set; }
    public DocumentoFiscal? DocumentoFiscal { get; set; }

    public TipoEventoFiscal TipoEvento { get; set; }
    // EMISSAO, CANCELAMENTO, CONSULTA, REJEICAO, SINCRONIZACAO

    public StatusEventoFiscal Status { get; set; }
    // SUCESSO, ERRO, PROCESSANDO

    public string? Protocolo { get; set; }
    public string? Mensagem { get; set; }

    public string? PayloadEnvio { get; set; }
    public string? PayloadRetorno { get; set; }

    public DateTime DataEvento { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }
    public Usuario? UsuarioCriacao { get; set; }
}