using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = "OwnerOuSuperAdmin")]
public class UsuariosController : ApiTenantControllerBase
{
    private readonly IUsuarioService _service;

    public UsuariosController(IUsuarioService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar(
        [FromBody] CreateUsuarioDto dto,
        CancellationToken cancellationToken)
    {
        var usuario = await _service.CriarAsync(
            dto,
            EhSuperAdmin() ? null : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = usuario.Id }, usuario);
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar(CancellationToken cancellationToken)
    {
        var usuarios = await _service.ListarAsync(
            EhSuperAdmin() ? null : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        return Ok(usuarios);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await _service.ObterPorIdAsync(
            id,
            EhSuperAdmin() ? null : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        if (usuario is null)
            return NotFound();

        return Ok(usuario);
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        var inativado = await _service.InativarAsync(
            id,
            ObterUsuarioId(),
            EhSuperAdmin() ? null : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        if (!inativado)
            return NotFound();

        return NoContent();
    }
}
