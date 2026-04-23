using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs.Fornecedores;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/fornecedores")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel3")]
public class FornecedoresController : ApiTenantControllerBase
{
    private readonly IFornecedorService _service;

    public FornecedoresController(IFornecedorService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<FornecedorDto>> Criar([FromBody] CreateFornecedorDto dto, CancellationToken cancellationToken)
    {
        var fornecedor = await _service.CriarAsync(ObterEmpresaId(), dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = fornecedor.Id }, fornecedor);
    }

    [HttpGet]
    public async Task<ActionResult<List<FornecedorDto>>> Listar(CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarAsync(ObterEmpresaId(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FornecedorDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var fornecedor = await _service.ObterPorIdAsync(ObterEmpresaId(), id, cancellationToken);
        return fornecedor is null ? NotFound(new { message = "Fornecedor nao encontrado." }) : Ok(fornecedor);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FornecedorDto>> Atualizar(Guid id, [FromBody] UpdateFornecedorDto dto, CancellationToken cancellationToken)
    {
        var fornecedor = await _service.AtualizarAsync(ObterEmpresaId(), id, dto, cancellationToken);
        return fornecedor is null ? NotFound(new { message = "Fornecedor nao encontrado." }) : Ok(fornecedor);
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _service.InativarAsync(ObterEmpresaId(), id, cancellationToken);
        return ok ? Ok(new { message = "Fornecedor inativado com sucesso." }) : NotFound(new { message = "Fornecedor nao encontrado." });
    }

    [HttpGet("{id:guid}/mensagens")]
    public async Task<ActionResult<List<FornecedorMensagemHistoricoDto>>> ListarMensagens(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarMensagensAsync(ObterEmpresaId(), id, cancellationToken));
    }

    [HttpPost("{id:guid}/mensagens")]
    public async Task<ActionResult<FornecedorMensagemHistoricoDto>> RegistrarMensagem(
        Guid id,
        [FromBody] CreateFornecedorMensagemHistoricoDto dto,
        CancellationToken cancellationToken)
    {
        var historico = await _service.RegistrarMensagemAsync(ObterEmpresaId(), id, dto, cancellationToken);
        return CreatedAtAction(nameof(ListarMensagens), new { id }, historico);
    }
}
