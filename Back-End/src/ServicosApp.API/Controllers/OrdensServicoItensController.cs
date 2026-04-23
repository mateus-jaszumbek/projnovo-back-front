using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/ordens-servico/{ordemServicoId:guid}/itens")]
public class OrdensServicoItensController : ApiTenantControllerBase
{
    private readonly IOrdemServicoItemService _service;

    public OrdensServicoItensController(IOrdemServicoItemService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<OrdemServicoItemDto>> Adicionar(Guid ordemServicoId, [FromBody] CreateOrdemServicoItemDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.AdicionarAsync(empresaId, ordemServicoId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<OrdemServicoItemDto>>> Listar(Guid ordemServicoId, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ListarAsync(empresaId, ordemServicoId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{itemId:guid}")]
    public async Task<ActionResult<OrdemServicoItemDto>> Atualizar(Guid ordemServicoId, Guid itemId, [FromBody] UpdateOrdemServicoItemDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.AtualizarAsync(empresaId, ordemServicoId, itemId, dto, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Item não encontrado." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Remover(Guid ordemServicoId, Guid itemId, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var ok = await _service.RemoverAsync(empresaId, ordemServicoId, itemId, cancellationToken);

            if (!ok)
                return NotFound(new { message = "Item não encontrado." });

            return Ok(new { message = "Item removido com sucesso." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("ordem")]
    public async Task<IActionResult> Reordenar(Guid ordemServicoId, [FromBody] List<ReordenarOrdemServicoItemDto> itens, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var ok = await _service.ReordenarAsync(empresaId, ordemServicoId, itens, cancellationToken);
            return ok ? NoContent() : NotFound(new { message = "Itens não encontrados." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
