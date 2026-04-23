using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/aparelhos")]
public class AparelhosController : ApiTenantControllerBase
{
    private readonly IAparelhoService _service;
    private readonly IImeiLookupService _imeiLookupService;

    public AparelhosController(IAparelhoService service, IImeiLookupService imeiLookupService)
    {
        _service = service;
        _imeiLookupService = imeiLookupService;
    }

    [AllowAnonymous]
    [HttpGet("imei/{imei}")]
    public async Task<ActionResult<ImeiLookupDto>> ConsultarImei(
        string imei,
        CancellationToken cancellationToken)
    {
        var result = await _imeiLookupService.ConsultarAsync(imei, cancellationToken);
        return result.Valido ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    public async Task<ActionResult<AparelhoDto>> Post(
        [FromBody] CreateAparelhoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.CriarAsync(empresaId, dto, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<AparelhoDto>>> Get(
        [FromQuery] Guid? clienteId,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = clienteId.HasValue
            ? await _service.ListarPorClienteAsync(empresaId, clienteId.Value, cancellationToken)
            : await _service.ListarAsync(empresaId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AparelhoDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterPorIdAsync(empresaId, id, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Aparelho nao encontrado." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AparelhoDto>> Put(
        Guid id,
        [FromBody] UpdateAparelhoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.AtualizarAsync(empresaId, id, dto, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Aparelho nao encontrado." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var ok = await _service.InativarAsync(empresaId, id, cancellationToken);

        if (!ok)
            return NotFound(new { message = "Aparelho nao encontrado." });

        return NoContent();
    }
}
