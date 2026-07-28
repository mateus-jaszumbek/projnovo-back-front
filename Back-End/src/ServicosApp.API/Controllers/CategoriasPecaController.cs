using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/categorias-peca")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel3")]
public class CategoriasPecaController : ApiTenantControllerBase
{
    private readonly ICategoriaPecaService _service;

    public CategoriasPecaController(ICategoriaPecaService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaPecaDto>> Criar([FromBody] CreateCategoriaPecaDto dto, CancellationToken cancellationToken)
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
    public async Task<ActionResult<List<CategoriaPecaDto>>> Listar([FromQuery] bool? ativo, [FromQuery] string? busca, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ListarAsync(empresaId, ativo, busca, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoriaPecaDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterPorIdAsync(empresaId, id, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Categoria não encontrada." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoriaPecaDto>> Atualizar(Guid id, [FromBody] UpdateCategoriaPecaDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.AtualizarAsync(empresaId, id, dto, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Categoria não encontrada." });

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
            return NotFound(new { message = "Categoria não encontrada." });

        return Ok(new { message = "Categoria inativada com sucesso." });
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var ok = await _service.AtivarAsync(empresaId, id, cancellationToken);

        if (!ok)
            return NotFound(new { message = "Categoria não encontrada." });

        return Ok(new { message = "Categoria ativada com sucesso." });
    }
}
