using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/contas-pagar")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel4")]
public class ContasPagarController : ApiTenantControllerBase
{
    private readonly IContaPagarService _service;

    public ContasPagarController(IContaPagarService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ContaPagarDto>> Criar(
        [FromBody] CreateContaPagarDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.CriarAsync(ObterEmpresaId(), dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<ContaPagarDto>>> Listar(CancellationToken cancellationToken)
    {
        var result = await _service.ListarAsync(ObterEmpresaId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContaPagarDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ObterPorIdAsync(ObterEmpresaId(), id, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPatch("{id:guid}/pagar")]
    public async Task<ActionResult<ContaPagarDto>> Pagar(
        Guid id,
        [FromBody] PagarContaPagarDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.PagarAsync(ObterEmpresaId(), ObterUsuarioId(), id, dto, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
