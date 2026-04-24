using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface INfseService
{
    Task<DocumentoFiscalDto> EmitirPorOrdemServicoAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid ordemServicoId,
        EmitirNfsePorOsDto dto,
        CancellationToken cancellationToken = default);

    Task<List<DocumentoFiscalDto>> ListarAsync(
        Guid empresaId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<DocumentoFiscalDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default);

    Task<DocumentoFiscalDto> ConsultarAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default);

    Task<DocumentoFiscalDto> CancelarAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid documentoFiscalId,
        CancelarDocumentoFiscalDto dto,
        CancellationToken cancellationToken = default);

    Task<DocumentoFiscalWebhookReplayDto> SolicitarReenvioWebhookAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default);
}
