using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IConfiguracaoFiscalService
{
    Task<ConfiguracaoFiscalDto> SalvarAsync(Guid empresaId, UpdateConfiguracaoFiscalDto dto, CancellationToken cancellationToken = default);
    Task<ConfiguracaoFiscalDto?> ObterAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<FocusNfseMunicipioValidacaoDto> ValidarMunicipioFocusNfseAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<FocusWebhookSetupDto> ObterFocusWebhookSetupAsync(Guid empresaId, string? requestBaseUrl, CancellationToken cancellationToken = default);
    Task<FiscalReadinessDto> ObterChecklistAsync(Guid empresaId, string? requestBaseUrl, CancellationToken cancellationToken = default);
}
