using System.Text.Json;
using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IFocusFiscalWebhookService
{
    bool IsRequestAuthorized(string? providedSecret);

    Task<FocusWebhookProcessResultDto> ProcessDfeAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default);

    Task<FocusWebhookProcessResultDto> ProcessNfseAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default);
}
