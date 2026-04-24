using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/kanban")]
[Authorize(Policy = "Nivel2")]
public class KanbanController : ApiTenantControllerBase
{
    private readonly IKanbanService _service;

    public KanbanController(IKanbanService service)
    {
        _service = service;
    }

    [HttpGet("publico")]
    public async Task<ActionResult<List<KanbanPublicoColunaDto>>> ObterQuadroPublico(CancellationToken cancellationToken)
    {
        return Ok(await _service.ObterQuadroPublicoAsync(ObterEmpresaId(), cancellationToken));
    }

    [HttpGet("publico/encerrados")]
    public async Task<ActionResult<List<KanbanPublicoCardDto>>> ListarEncerradosPublico(CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarEncerradosPublicoAsync(ObterEmpresaId(), cancellationToken));
    }

    [HttpGet("publico/configuracao")]
    public async Task<ActionResult<List<KanbanConfiguracaoColunaDto>>> ObterConfiguracaoPublica(CancellationToken cancellationToken)
    {
        return Ok(await _service.ObterConfiguracaoPublicaAsync(ObterEmpresaId(), cancellationToken));
    }

    [HttpPost("publico/colunas")]
    public async Task<ActionResult<KanbanConfiguracaoColunaDto>> CriarColunaPublica(
        [FromBody] CreateKanbanPublicoColunaDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.CriarColunaPublicaAsync(ObterEmpresaId(), dto, cancellationToken));
    }

    [HttpPut("publico/colunas/{id:guid}")]
    public async Task<ActionResult<KanbanConfiguracaoColunaDto>> AtualizarColunaPublica(
        Guid id,
        [FromBody] UpdateKanbanPublicoColunaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarColunaPublicaAsync(ObterEmpresaId(), id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("publico/colunas/{id:guid}/reordenar")]
    public async Task<IActionResult> ReordenarColunaPublica(
        Guid id,
        [FromBody] ReordenarKanbanColunaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.ReordenarColunaPublicaAsync(ObterEmpresaId(), id, dto, cancellationToken);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("publico/colunas/{id:guid}")]
    public async Task<IActionResult> ExcluirColunaPublica(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ExcluirColunaPublicaAsync(ObterEmpresaId(), id, cancellationToken);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("publico/colunas/{id:guid}/permanente")]
    public async Task<IActionResult> ExcluirColunaPublicaPermanentemente(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ExcluirColunaPublicaPermanentementeAsync(ObterEmpresaId(), id, cancellationToken);
        return result ? NoContent() : NotFound();
    }

    [HttpPatch("publico/os/{ordemServicoId:guid}/mover")]
    public async Task<ActionResult<KanbanPublicoCardDto>> MoverCardPublico(
        Guid ordemServicoId,
        [FromBody] MoveKanbanPublicoCardDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.MoverCardPublicoAsync(
            ObterEmpresaId(),
            ordemServicoId,
            dto,
            ObterUsuarioId(),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("publico/os/{ordemServicoId:guid}/reabrir")]
    public async Task<ActionResult<KanbanPublicoCardDto>> ReabrirCardPublico(
        Guid ordemServicoId,
        CancellationToken cancellationToken)
    {
        var result = await _service.ReabrirCardPublicoAsync(
            ObterEmpresaId(),
            ordemServicoId,
            ObterUsuarioId(),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("privado")]
    public async Task<ActionResult<List<KanbanPrivadoColunaDto>>> ObterMeuKanbanPrivado(CancellationToken cancellationToken)
    {
        return Ok(await _service.ObterMeuKanbanPrivadoAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            cancellationToken));
    }

    [HttpGet("privado/configuracao")]
    public async Task<ActionResult<List<KanbanPrivadoColunaDto>>> ObterConfiguracaoPrivada(CancellationToken cancellationToken)
    {
        return Ok(await _service.ObterConfiguracaoPrivadaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            cancellationToken));
    }

    [HttpPost("privado/colunas")]
    public async Task<ActionResult<KanbanPrivadoColunaDto>> CriarColunaPrivada(
        [FromBody] CreateKanbanPrivadoColunaDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.CriarColunaPrivadaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            dto,
            cancellationToken));
    }

    [HttpPut("privado/colunas/{id:guid}")]
    public async Task<ActionResult<KanbanPrivadoColunaDto>> AtualizarColunaPrivada(
        Guid id,
        [FromBody] UpdateKanbanPrivadoColunaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarColunaPrivadaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            id,
            dto,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("privado/colunas/{id:guid}")]
    public async Task<IActionResult> ExcluirColunaPrivada(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ExcluirColunaPrivadaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            id,
            cancellationToken);

        return result ? NoContent() : NotFound();
    }

    [HttpDelete("privado/colunas/{id:guid}/permanente")]
    public async Task<IActionResult> ExcluirColunaPrivadaPermanentemente(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ExcluirColunaPrivadaPermanentementeAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            id,
            cancellationToken);

        return result ? NoContent() : NotFound();
    }

    [HttpPost("privado/tarefas")]
    public async Task<ActionResult<KanbanPrivadoCardDto>> CriarTarefaPrivada(
        [FromBody] CreateKanbanTarefaPrivadaDto dto,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.CriarTarefaPrivadaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            dto,
            cancellationToken));
    }

    [HttpPut("privado/tarefas/{id:guid}")]
    public async Task<ActionResult<KanbanPrivadoCardDto>> AtualizarTarefaPrivada(
        Guid id,
        [FromBody] UpdateKanbanTarefaPrivadaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarTarefaPrivadaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            id,
            dto,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("privado/tarefas/{id:guid}/mover")]
    public async Task<ActionResult<KanbanPrivadoCardDto>> MoverTarefaPrivada(
        Guid id,
        [FromBody] MoveKanbanTarefaPrivadaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.MoverTarefaPrivadaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            id,
            dto,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("privado/tarefas/{id:guid}")]
    public async Task<IActionResult> ExcluirTarefaPrivada(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ExcluirTarefaPrivadaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            id,
            cancellationToken);

        return result ? NoContent() : NotFound();
    }
}
