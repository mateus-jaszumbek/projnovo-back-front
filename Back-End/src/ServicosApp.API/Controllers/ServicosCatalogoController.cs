using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/servicos-catalogo")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel2")]
public class ServicosCatalogoController : ApiTenantControllerBase
{
    private readonly IServicoCatalogoService _service;

    public ServicosCatalogoController(IServicoCatalogoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ServicoCatalogoDto>> Criar(
        [FromBody] CreateServicoCatalogoDto dto,
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
    public async Task<ActionResult<List<ServicoCatalogoDto>>> Listar(
        [FromQuery] bool? ativo,
        [FromQuery] string? busca,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ListarAsync(empresaId, ativo, busca, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServicoCatalogoDto>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterPorIdAsync(empresaId, id, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Serviço não encontrado." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServicoCatalogoDto>> Atualizar(
        Guid id,
        [FromBody] UpdateServicoCatalogoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.AtualizarAsync(empresaId, id, dto, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Serviço não encontrado." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var ok = await _service.InativarAsync(empresaId, id, cancellationToken);

        if (!ok)
            return NotFound(new { message = "Serviço não encontrado." });

        return Ok(new { message = "Serviço inativado com sucesso." });
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var ok = await _service.AtivarAsync(empresaId, id, cancellationToken);

        if (!ok)
            return NotFound(new { message = "Serviço não encontrado." });

        return Ok(new { message = "Serviço ativado com sucesso." });
    }
}
