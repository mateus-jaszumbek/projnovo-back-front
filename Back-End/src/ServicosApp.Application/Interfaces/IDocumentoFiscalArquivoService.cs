using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IDocumentoFiscalArquivoService
{
    Task<DocumentoFiscalArquivoDto?> ObterXmlAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default);

    Task<DocumentoFiscalImpressaoDto?> ObterImpressaoAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default);
}
