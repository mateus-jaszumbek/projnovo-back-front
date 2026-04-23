using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/public/ordens-servico")]
public class OrdemServicoTrackingPublicoController : ControllerBase
{
    private readonly IKanbanService _service;

    public OrdemServicoTrackingPublicoController(IKanbanService service)
    {
        _service = service;
    }

    [HttpGet("{token}/acompanhamento")]
    [AllowAnonymous]
    public async Task<ActionResult<KanbanTrackingPublicoDto>> ObterAcompanhamento(
        string token,
        CancellationToken cancellationToken)
    {
        var result = await _service.ObterTrackingPublicoAsync(token, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}