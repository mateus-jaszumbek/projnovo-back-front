using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ApiTenantControllerBase
{
    private readonly IClienteService _service;

    public ClientesController(IClienteService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Criar(
        [FromBody] CreateClienteDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var cliente = await _service.CriarAsync(empresaId, dto, cancellationToken);
            return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id }, cliente);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> Listar(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var clientes = await _service.ListarAsync(empresaId, cancellationToken);
        return Ok(clientes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var cliente = await _service.ObterPorIdAsync(empresaId, id, cancellationToken);

        if (cliente is null)
            return NotFound(new { message = "Cliente não encontrado." });

        return Ok(cliente);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> Atualizar(
        Guid id,
        [FromBody] UpdateClienteDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var cliente = await _service.AtualizarAsync(empresaId, id, dto, cancellationToken);

            if (cliente is null)
                return NotFound(new { message = "Cliente não encontrado." });

            return Ok(cliente);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/inativar")]
    public async Task<IActionResult> Inativar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var ok = await _service.InativarAsync(empresaId, id, cancellationToken);

        if (!ok)
            return NotFound(new { message = "Cliente não encontrado." });

        return Ok(new { message = "Cliente inativado com sucesso." });
    }
}
