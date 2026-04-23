using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/configuracao-fiscal")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel5")]
public class ConfiguracaoFiscalController : ApiTenantControllerBase
{
    private readonly IConfiguracaoFiscalService _service;

    public ConfiguracaoFiscalController(IConfiguracaoFiscalService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ConfiguracaoFiscalDto>> Obter(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.ObterAsync(empresaId, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Configuração fiscal não encontrada." });

        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<ConfiguracaoFiscalDto>> Salvar([FromBody] UpdateConfiguracaoFiscalDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.SalvarAsync(empresaId, dto, cancellationToken);
        return Ok(result);
    }


}
