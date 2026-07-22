using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/public/clientes")]
public class ClientePortalPublicoController : ControllerBase
{
    private readonly IClienteService _service;

    public ClientePortalPublicoController(IClienteService service)
    {
        _service = service;
    }

    [HttpGet("{token}/portal")]
    [AllowAnonymous]
    public async Task<ActionResult<ClientePortalDto>> ObterPortal(
        string token,
        CancellationToken cancellationToken)
    {
        var result = await _service.ObterPortalPublicoAsync(token, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
