using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/ordens-servico")]
public class OrdensServicoController : ApiTenantControllerBase
{
    private readonly IOrdemServicoService _service;

    public OrdensServicoController(IOrdemServicoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<OrdemServicoDto>> Criar(
        [FromBody] CreateOrdemServicoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.CriarAsync(empresaId, dto, cancellationToken);
            return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<OrdemServicoDto>>> Listar(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ListarAsync(empresaId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrdemServicoDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterPorIdAsync(empresaId, id, cancellationToken);

        if (result is null)
            return NotFound(new { message = "OS não encontrada." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrdemServicoDto>> Atualizar(
        Guid id,
        [FromBody] UpdateOrdemServicoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.AtualizarAsync(empresaId, id, dto, cancellationToken);

            if (result is null)
                return NotFound(new { message = "OS não encontrada." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrdemServicoDto>> AlterarStatus(
        Guid id,
        [FromBody] AlterarStatusOrdemServicoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.AlterarStatusAsync(empresaId, id, dto, cancellationToken);

        if (result is null)
            return NotFound(new { message = "OS não encontrada." });

        return Ok(result);
    }

    [HttpPatch("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var usuarioId = ObterUsuarioId();

        var ok = await _service.CancelarAsync(empresaId, usuarioId, id, cancellationToken);

        if (!ok)
            return NotFound(new { message = "OS não encontrada." });

        return Ok(new { message = "OS cancelada com sucesso." });
    }
}