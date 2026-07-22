using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/configuracao-pagamento")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel5")]
public class ConfiguracaoPagamentoController : ApiTenantControllerBase
{
    private readonly IConfiguracaoPagamentoService _service;

    public ConfiguracaoPagamentoController(IConfiguracaoPagamentoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ConfiguracaoPagamentoDto>> Obter(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterAsync(empresaId, BuildRequestBaseUrl(), cancellationToken);
        if (result is null)
            return NotFound(new { message = "Configuração de pagamento não encontrada." });

        return Ok(result);
    }

    [HttpGet("provedores")]
    public ActionResult<IReadOnlyCollection<PagamentoProviderInfoDto>> ListarProvedores()
    {
        return Ok(_service.ListarProvedoresDisponiveis());
    }

    [HttpPut]
    public async Task<ActionResult<ConfiguracaoPagamentoDto>> Salvar(
        [FromBody] UpdateConfiguracaoPagamentoDto dto,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        try
        {
            var result = await _service.SalvarAsync(empresaId, dto, BuildRequestBaseUrl(), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string BuildRequestBaseUrl()
        => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
}
