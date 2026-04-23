using ServicosApp.Application.DTOs;
using ServicosApp.Domain.Enums;

namespace ServicosApp.Application.Interfaces;

public interface IDocumentoFiscalConsultaService
{
    Task<List<DocumentoFiscalDto>> ListarAsync(
        Guid empresaId,
        string? tipoDocumento,
        string? status,
        CancellationToken cancellationToken = default);

    Task<DocumentoFiscalDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default);

    Task<TipoDocumentoFiscal?> ObterTipoDocumentoAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default);
}
