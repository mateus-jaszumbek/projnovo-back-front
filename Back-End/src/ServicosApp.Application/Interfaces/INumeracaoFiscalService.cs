using ServicosApp.Domain.Enums;

namespace ServicosApp.Application.Interfaces;

public interface INumeracaoFiscalService
{
    Task<(int serie, long numero, string? serieRps, long? numeroRps)> ReservarNumeracaoNfseAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    Task<(int serie, long numero)> ReservarNumeracaoAsync(
        Guid empresaId,
        TipoDocumentoFiscal tipoDocumento,
        CancellationToken cancellationToken = default);
}
