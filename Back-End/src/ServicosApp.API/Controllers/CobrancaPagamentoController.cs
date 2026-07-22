using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/cobrancas-pagamento")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel2")]
public class CobrancaPagamentoController : ApiTenantControllerBase
{
    private readonly ICobrancaPagamentoService _service;

    public CobrancaPagamentoController(ICobrancaPagamentoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CobrancaPagamentoDto>> Criar(
        [FromBody] CreateCobrancaPagamentoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var usuarioId = ObterUsuarioId();

        try
        {
            var result = await _service.CriarAsync(empresaId, usuarioId, dto, BuildRequestBaseUrl(), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CobrancaPagamentoDto>> Obter(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterAsync(empresaId, id, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Cobrança não encontrada." });

        return Ok(result);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<CobrancaPagamentoDto>> ConsultarStatus(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ConsultarStatusAsync(empresaId, id, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Cobrança não encontrada." });

        return Ok(result);
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.CancelarAsync(empresaId, id, cancellationToken);
        if (!result)
            return NotFound(new { message = "Cobrança não encontrada." });

        return Ok(new { message = "Cobrança cancelada." });
    }

    private string BuildRequestBaseUrl()
        => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
}
