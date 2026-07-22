using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/public/pagamentos/webhooks")]
[AllowAnonymous]
public class PagamentoWebhooksController : ControllerBase
{
    private readonly ICobrancaPagamentoService _service;

    public PagamentoWebhooksController(ICobrancaPagamentoService service)
    {
        _service = service;
    }

    [HttpPost("{provider}/{secret}")]
    public async Task<ActionResult> Receber(string provider, string secret, CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);

        await _service.ProcessarWebhookAsync(provider, secret, rawPayload, cancellationToken);

        return Ok();
    }
}
