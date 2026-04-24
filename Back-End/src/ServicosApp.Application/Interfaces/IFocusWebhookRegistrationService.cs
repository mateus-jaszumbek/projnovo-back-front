using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IFocusWebhookRegistrationService
{
    Task<FocusWebhookSetupDto> ObterStatusAsync(Guid empresaId, string? requestBaseUrl, CancellationToken cancellationToken = default);
    Task<FocusWebhookSetupDto> SincronizarAsync(Guid empresaId, string? requestBaseUrl, CancellationToken cancellationToken = default);
}
