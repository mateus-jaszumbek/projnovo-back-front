using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/estoque")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel3")]
public class EstoqueController : ApiTenantControllerBase
{
    private readonly IEstoqueMovimentoService _service;

    public EstoqueController(IEstoqueMovimentoService service)
    {
        _service = service;
    }

    [HttpPost("entradas")]
    public async Task<ActionResult<EstoqueMovimentoDto>> RegistrarEntrada([FromBody] CreateEstoqueEntradaDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.RegistrarEntradaAsync(empresaId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("saidas")]
    public async Task<ActionResult<EstoqueMovimentoDto>> RegistrarSaida([FromBody] CreateEstoqueSaidaDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.RegistrarSaidaAsync(empresaId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("consumos-os")]
    public async Task<ActionResult<EstoqueMovimentoDto>> RegistrarConsumoOs([FromBody] CreateConsumoOrdemServicoDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.RegistrarConsumoOrdemServicoAsync(empresaId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pecas/{pecaId:guid}/movimentos")]
    public async Task<ActionResult<List<EstoqueMovimentoDto>>> ListarPorPeca(Guid pecaId, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ListarPorPecaAsync(empresaId, pecaId, cancellationToken);
        return Ok(result);
    }
}
