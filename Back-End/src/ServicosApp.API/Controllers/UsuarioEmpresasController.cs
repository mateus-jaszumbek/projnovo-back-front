using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/usuario-empresas")]
[Authorize(Policy = "OwnerOuSuperAdmin")]
public class UsuarioEmpresasController : ApiTenantControllerBase
{
    private readonly IUsuarioEmpresaService _service;

    public UsuarioEmpresasController(IUsuarioEmpresaService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioEmpresaDto>> Criar(
        [FromBody] CreateUsuarioEmpresaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.CriarAsync(
            dto,
            EhSuperAdmin() ? Guid.Empty : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioEmpresaDto>>> Listar(CancellationToken cancellationToken)
    {
        var result = await _service.ListarAsync(
            EhSuperAdmin() ? Guid.Empty : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UsuarioEmpresaDto>> Atualizar(
        Guid id,
        [FromBody] UpdateUsuarioEmpresaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarAsync(
            id,
            dto,
            ObterUsuarioId(),
            EhSuperAdmin() ? Guid.Empty : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        var inativado = await _service.InativarAsync(
            id,
            ObterUsuarioId(),
            EhSuperAdmin() ? Guid.Empty : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        if (!inativado)
            return NotFound();

        return NoContent();
    }
}
