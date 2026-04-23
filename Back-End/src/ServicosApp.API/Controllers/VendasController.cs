using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/vendas")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel2")]
public class VendasController : ApiTenantControllerBase
{
    private readonly IVendaService _service;

    public VendasController(IVendaService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<VendaDto>> Criar(
        [FromBody] CreateVendaDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var usuarioId = ObterUsuarioId();

        try
        {
            var result = await _service.CriarAsync(empresaId, usuarioId, dto, cancellationToken);
            return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("com-itens")]
    public async Task<ActionResult<VendaDto>> CriarComItens(
        [FromBody] CreateVendaComItensDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var usuarioId = ObterUsuarioId();

        try
        {
            var result = await _service.CriarComItensAsync(empresaId, usuarioId, dto, cancellationToken);
            return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<VendaDto>>> Listar(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var result = await _service.ListarAsync(empresaId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VendaDto>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterPorIdAsync(empresaId, id, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Venda não encontrada." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VendaDto>> Atualizar(
        Guid id,
        [FromBody] UpdateVendaDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.AtualizarAsync(empresaId, id, dto, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Venda não encontrada." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/finalizar")]
    public async Task<IActionResult> Finalizar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var ok = await _service.FinalizarAsync(empresaId, ObterUsuarioId(), id, cancellationToken);

            if (!ok)
                return NotFound(new { message = "Venda não encontrada." });

            return Ok(new { message = "Venda finalizada com sucesso." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var ok = await _service.CancelarAsync(empresaId, id, cancellationToken);

            if (!ok)
                return NotFound(new { message = "Venda não encontrada." });

            return Ok(new { message = "Venda cancelada com sucesso." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
