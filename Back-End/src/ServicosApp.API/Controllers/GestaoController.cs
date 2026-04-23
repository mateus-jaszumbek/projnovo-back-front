using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/gestao")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel3")]
public class GestaoController : ApiTenantControllerBase
{
    private readonly IGestaoService _service;

    public GestaoController(IGestaoService service)
    {
        _service = service;
    }

    [HttpPost("compras-pecas")]
    public async Task<IActionResult> ComprarPeca([FromBody] CompraPecaDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await _service.RegistrarCompraPecaAsync(ObterEmpresaId(), ObterUsuarioId(), dto, cancellationToken);
            return Ok(new { message = "Compra registrada, estoque atualizado e conta a pagar criada quando solicitado." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("dre")]
    public async Task<ActionResult<List<DreGerencialDto>>> Dre([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken cancellationToken)
    {
        return Ok(new[] { await _service.ObterDreAsync(ObterEmpresaId(), inicio, fim, cancellationToken) });
    }

    [HttpGet("comissoes")]
    public async Task<ActionResult<List<ComissaoDto>>> Comissoes(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        [FromQuery] decimal percentualVendas = 2,
        [FromQuery] decimal percentualServicos = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.ListarComissoesAsync(ObterEmpresaId(), inicio, fim, percentualVendas, percentualServicos, cancellationToken));
    }

    [HttpGet("auditoria-financeira")]
    public async Task<ActionResult<List<AuditoriaFinanceiraDto>>> AuditoriaFinanceira(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarAuditoriaFinanceiraAsync(ObterEmpresaId(), inicio, fim, cancellationToken));
    }
}
