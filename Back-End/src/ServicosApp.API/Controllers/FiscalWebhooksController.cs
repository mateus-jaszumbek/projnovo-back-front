using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/fiscal/webhooks/focus")]
[AllowAnonymous]
public class FiscalWebhooksController : ControllerBase
{
    private readonly IFocusFiscalWebhookService _focusWebhookService;

    public FiscalWebhooksController(IFocusFiscalWebhookService focusWebhookService)
    {
        _focusWebhookService = focusWebhookService;
    }

    [HttpPost("dfe/{secret}")]
    public Task<ActionResult<FocusWebhookProcessResultDto>> ReceiveDfe(
        string secret,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        return ProcessAsync(
            secret,
            payload,
            isDfe: true,
            cancellationToken);
    }

    [HttpPost("nfse/{secret}")]
    public Task<ActionResult<FocusWebhookProcessResultDto>> ReceiveNfse(
        string secret,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        return ProcessAsync(
            secret,
            payload,
            isDfe: false,
            cancellationToken);
    }

    private async Task<ActionResult<FocusWebhookProcessResultDto>> ProcessAsync(
        string secret,
        JsonElement payload,
        bool isDfe,
        CancellationToken cancellationToken)
    {
        if (!_focusWebhookService.IsRequestAuthorized(secret))
            return Unauthorized(new { message = "Webhook da Focus nao autorizado." });

        var result = isDfe
            ? await _focusWebhookService.ProcessDfeAsync(payload, cancellationToken)
            : await _focusWebhookService.ProcessNfseAsync(payload, cancellationToken);

        return Ok(result);
    }
}
