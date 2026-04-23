using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/regras-fiscais-produtos")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel5")]
public class RegrasFiscaisProdutosController : ApiTenantControllerBase
{
    private readonly IRegraFiscalProdutoService _service;

    public RegrasFiscaisProdutosController(IRegraFiscalProdutoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<RegraFiscalProdutoDto>> Criar(
        [FromBody] CreateRegraFiscalProdutoDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.CriarAsync(ObterEmpresaId(), dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<RegraFiscalProdutoDto>>> Listar(
        [FromQuery] string? tipoDocumentoFiscal,
        [FromQuery] bool? ativo,
        CancellationToken cancellationToken)
    {
        var result = await _service.ListarAsync(
            ObterEmpresaId(),
            tipoDocumentoFiscal,
            ativo,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RegraFiscalProdutoDto>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.ObterPorIdAsync(ObterEmpresaId(), id, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Regra fiscal não encontrada." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RegraFiscalProdutoDto>> Atualizar(
        Guid id,
        [FromBody] UpdateRegraFiscalProdutoDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _service.AtualizarAsync(ObterEmpresaId(), id, dto, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Regra fiscal não encontrada." });

        return Ok(result);
    }
}
