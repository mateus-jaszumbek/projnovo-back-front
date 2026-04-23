using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/contas-receber")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel4")]
public class ContasReceberController : ApiTenantControllerBase
{
    private readonly IContaReceberService _service;

    public ContasReceberController(IContaReceberService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ContaReceberDto>> Criar([FromBody] CreateContaReceberDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.CriarAsync(empresaId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ContaReceberDto>>> Listar(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        return Ok(await _service.ListarAsync(empresaId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContaReceberDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterPorIdAsync(empresaId, id, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Conta a receber não encontrada." });

        return Ok(result);
    }

    [HttpPatch("{id:guid}/receber")]
    public async Task<ActionResult<ContaReceberDto>> Receber(Guid id, [FromBody] ReceberContaReceberDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.ReceberAsync(empresaId, ObterUsuarioId(), id, dto, cancellationToken);
            if (result is null)
                return NotFound(new { message = "Conta a receber não encontrada." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
