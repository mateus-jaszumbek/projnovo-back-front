using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/modulos-personalizados")]
public class ModulosPersonalizadosController : ApiTenantControllerBase
{
    private readonly IModuloPersonalizadoService _service;

    public ModulosPersonalizadosController(IModuloPersonalizadoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ModuloPersonalizadoDto>>> Listar(CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarModulosAsync(ObterEmpresaId(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ModuloPersonalizadoDto>> Obter(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ObterModuloAsync(ObterEmpresaId(), id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("chave/{chave}")]
    public async Task<ActionResult<ModuloPersonalizadoDto>> ObterPorChave(string chave, CancellationToken cancellationToken)
    {
        var result = await _service.ObterModuloPorChaveAsync(ObterEmpresaId(), chave, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "OwnerOuSuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<ModuloPersonalizadoDto>> Criar([FromBody] CreateModuloPersonalizadoDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _service.CriarModuloAsync(ObterEmpresaId(), dto, cancellationToken));
    }

    [HttpPost("sistema")]
    public async Task<ActionResult<ModuloPersonalizadoDto>> GarantirSistema([FromBody] EnsureModuloSistemaDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _service.GarantirModuloSistemaAsync(ObterEmpresaId(), dto, cancellationToken));
    }

    [Authorize(Policy = "OwnerOuSuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ModuloPersonalizadoDto>> Atualizar(Guid id, [FromBody] UpdateModuloPersonalizadoDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarModuloAsync(ObterEmpresaId(), id, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "OwnerOuSuperAdmin")]
    [HttpPost("{moduloId:guid}/campos")]
    public async Task<ActionResult<CampoPersonalizadoDto>> CriarCampo(Guid moduloId, [FromBody] CreateCampoPersonalizadoDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _service.CriarCampoAsync(ObterEmpresaId(), moduloId, dto, cancellationToken));
    }

    [Authorize(Policy = "OwnerOuSuperAdmin")]
    [HttpPut("{moduloId:guid}/campos/{campoId:guid}")]
    public async Task<ActionResult<CampoPersonalizadoDto>> AtualizarCampo(Guid moduloId, Guid campoId, [FromBody] UpdateCampoPersonalizadoDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarCampoAsync(ObterEmpresaId(), moduloId, campoId, dto, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "OwnerOuSuperAdmin")]
    [HttpDelete("{moduloId:guid}/campos/{campoId:guid}")]
    public async Task<IActionResult> ExcluirCampo(Guid moduloId, Guid campoId, CancellationToken cancellationToken)
    {
        var ok = await _service.ExcluirCampoAsync(ObterEmpresaId(), moduloId, campoId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [Authorize(Policy = "OwnerOuSuperAdmin")]
    [HttpPatch("{moduloId:guid}/campos/layout")]
    public async Task<IActionResult> ReordenarCampos(Guid moduloId, [FromBody] List<CampoLayoutDto> campos, CancellationToken cancellationToken)
    {
        var ok = await _service.ReordenarCamposAsync(ObterEmpresaId(), moduloId, campos, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("{moduloId:guid}/layout")]
    public async Task<ActionResult<List<CampoModuloLayoutDto>>> ListarLayout(Guid moduloId, CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarLayoutAsync(ObterEmpresaId(), moduloId, cancellationToken));
    }

    [Authorize(Policy = "OwnerOuSuperAdmin")]
    [HttpPatch("{moduloId:guid}/layout")]
    public async Task<IActionResult> SalvarLayout(Guid moduloId, [FromBody] List<CampoModuloLayoutDto> campos, CancellationToken cancellationToken)
    {
        var ok = await _service.SalvarLayoutAsync(ObterEmpresaId(), moduloId, campos, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("{moduloId:guid}/registros")]
    public async Task<ActionResult<List<RegistroPersonalizadoDto>>> ListarRegistros(Guid moduloId, CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarRegistrosAsync(ObterEmpresaId(), moduloId, cancellationToken));
    }

    [HttpPost("{moduloId:guid}/registros")]
    public async Task<ActionResult<RegistroPersonalizadoDto>> CriarRegistro(Guid moduloId, [FromBody] CreateRegistroPersonalizadoDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _service.CriarRegistroAsync(ObterEmpresaId(), moduloId, dto, cancellationToken));
    }

    [HttpPut("{moduloId:guid}/registros/origem/{origemId:guid}")]
    public async Task<ActionResult<RegistroPersonalizadoDto>> SalvarRegistroOrigem(Guid moduloId, Guid origemId, [FromBody] CreateRegistroPersonalizadoDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _service.SalvarRegistroOrigemAsync(ObterEmpresaId(), moduloId, origemId, dto, cancellationToken));
    }
}
