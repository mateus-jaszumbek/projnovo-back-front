using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/credenciais-fiscais")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel5")]
public class CredenciaisFiscaisController : ApiTenantControllerBase
{
    private readonly ICredencialFiscalEmpresaService _service;

    public CredenciaisFiscaisController(ICredencialFiscalEmpresaService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CredencialFiscalEmpresaDto>> Criar(
        [FromBody] CreateCredencialFiscalEmpresaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.CriarAsync(ObterEmpresaId(), dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<CredencialFiscalEmpresaDto>>> Listar(
        [FromQuery] string? tipoDocumentoFiscal,
        [FromQuery] bool? ativo,
        CancellationToken cancellationToken)
    {
        var result = await _service.ListarAsync(
            ObterEmpresaId(),
            tipoDocumentoFiscal,
            ativo,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CredencialFiscalEmpresaDto>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.ObterPorIdAsync(ObterEmpresaId(), id, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Credencial fiscal nao encontrada." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CredencialFiscalEmpresaDto>> Atualizar(
        Guid id,
        [FromBody] UpdateCredencialFiscalEmpresaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarAsync(ObterEmpresaId(), id, dto, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Credencial fiscal nao encontrada." });

        return Ok(result);
    }
}
