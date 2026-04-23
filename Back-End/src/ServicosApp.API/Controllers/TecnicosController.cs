using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel2")]
public class TecnicosController : ApiTenantControllerBase
{
    private readonly ITecnicoService _tecnicoService;

    public TecnicosController(ITecnicoService tecnicoService)
    {
        _tecnicoService = tecnicoService;
    }

    [HttpPost]
    public async Task<ActionResult<TecnicoDto>> Criar(
        [FromBody] CreateTecnicoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var tecnico = await _tecnicoService.CriarAsync(empresaId, dto, cancellationToken);
            return CreatedAtAction(nameof(ObterPorId), new { id = tecnico.Id }, tecnico);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<TecnicoDto>>> Listar(
        [FromQuery] bool? ativo,
        [FromQuery] string? busca,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var tecnicos = await _tecnicoService.ListarAsync(empresaId, ativo, busca, cancellationToken);
        return Ok(tecnicos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TecnicoDto>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var tecnico = await _tecnicoService.ObterPorIdAsync(empresaId, id, cancellationToken);

        if (tecnico is null)
            return NotFound(new { message = "Técnico não encontrado." });

        return Ok(tecnico);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TecnicoDto>> Atualizar(
        Guid id,
        [FromBody] UpdateTecnicoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var tecnico = await _tecnicoService.AtualizarAsync(empresaId, id, dto, cancellationToken);
            return Ok(tecnico);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var ok = await _tecnicoService.InativarAsync(empresaId, id, cancellationToken);

        if (!ok)
            return NotFound(new { message = "Técnico não encontrado." });

        return Ok(new { message = "Técnico inativado com sucesso." });
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var ok = await _tecnicoService.AtivarAsync(empresaId, id, cancellationToken);

        if (!ok)
            return NotFound(new { message = "Técnico não encontrado." });

        return Ok(new { message = "Técnico ativado com sucesso." });
    }

}
