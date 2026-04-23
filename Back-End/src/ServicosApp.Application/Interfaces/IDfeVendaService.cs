using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IDfeVendaService
{
    Task<DocumentoFiscalDto> EmitirNfePorVendaAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid vendaId,
        EmitirDfeVendaDto dto,
        CancellationToken cancellationToken = default);

    Task<DocumentoFiscalDto> EmitirNfcePorVendaAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid vendaId,
        EmitirDfeVendaDto dto,
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
}
