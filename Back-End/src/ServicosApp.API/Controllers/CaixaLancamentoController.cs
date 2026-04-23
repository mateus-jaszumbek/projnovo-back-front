using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/caixa-lancamentos")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel4")]
public class CaixaLancamentosController : ApiTenantControllerBase
{
    private readonly ICaixaLancamentoService _service;

    public CaixaLancamentosController(ICaixaLancamentoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CaixaLancamentoDto>> Lancar([FromBody] CreateCaixaLancamentoDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var usuarioId = ObterUsuarioId();

        try
        {
            var result = await _service.LancarAsync(empresaId, usuarioId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("caixa/{caixaDiarioId:guid}")]
    public async Task<ActionResult<List<CaixaLancamentoDto>>> ListarPorCaixa(Guid caixaDiarioId, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        return Ok(await _service.ListarPorCaixaAsync(empresaId, caixaDiarioId, cancellationToken));
    }
         
}
