using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/vendas/{vendaId:guid}/itens")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel2")]
public class VendaItensController : ApiTenantControllerBase
{
    private readonly IVendaItemService _service;

    public VendaItensController(IVendaItemService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<VendaItemDto>> Adicionar(Guid vendaId, [FromBody] CreateVendaItemDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.AdicionarAsync(empresaId, vendaId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<VendaItemDto>>> Listar(Guid vendaId, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        return Ok(await _service.ListarAsync(empresaId, vendaId, cancellationToken));
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Remover(Guid vendaId, Guid itemId, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var ok = await _service.RemoverAsync(empresaId, vendaId, itemId, cancellationToken);
            if (!ok)
                return NotFound(new { message = "Item não encontrado." });

            return Ok(new { message = "Item removido com sucesso." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
