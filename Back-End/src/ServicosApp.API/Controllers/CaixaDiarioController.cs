using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/caixas-diarios")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel4")]
public class CaixasDiariosController : ApiTenantControllerBase
{
    private readonly ICaixaDiarioService _service;

    public CaixasDiariosController(ICaixaDiarioService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CaixaDiarioDto>> Abrir([FromBody] CreateCaixaDiarioDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var usuarioId = ObterUsuarioId();

        try
        {
            var result = await _service.AbrirAsync(empresaId, usuarioId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<CaixaDiarioDto>>> Listar(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        return Ok(await _service.ListarAsync(empresaId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CaixaDiarioDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterPorIdAsync(empresaId, id, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Caixa não encontrado." });

        return Ok(result);
    }

    [HttpPatch("{id:guid}/fechar")]
    public async Task<ActionResult<CaixaDiarioDto>> Fechar(Guid id, [FromBody] FecharCaixaDiarioDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var usuarioId = ObterUsuarioId();

        try
        {
            var result = await _service.FecharAsync(empresaId, id, usuarioId, dto, cancellationToken);
            if (result is null)
                return NotFound(new { message = "Caixa não encontrado." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
