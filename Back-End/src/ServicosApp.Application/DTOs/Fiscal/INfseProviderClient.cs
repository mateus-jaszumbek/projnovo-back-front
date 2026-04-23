using ServicosApp.Domain.Entities;

namespace ServicosApp.Application.DTOs.Fiscal;

public interface INfseProviderClient
{
    Task<NfseProviderResult> EmitirAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default);

    Task<NfseProviderResult> ConsultarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default);

    Task<NfseProviderResult> CancelarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        string motivo,
        CancellationToken cancellationToken = default);
}