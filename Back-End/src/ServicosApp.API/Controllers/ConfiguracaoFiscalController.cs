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
    private readonly IFocusWebhookRegistrationService _focusWebhookRegistrationService;

    public ConfiguracaoFiscalController(
        IConfiguracaoFiscalService service,
        IFocusWebhookRegistrationService focusWebhookRegistrationService)
    {
        _service = service;
        _focusWebhookRegistrationService = focusWebhookRegistrationService;
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

    [HttpGet("checklist")]
    public async Task<ActionResult<FiscalReadinessDto>> ObterChecklist(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var result = await _service.ObterChecklistAsync(
            empresaId,
            BuildRequestBaseUrl(),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<ConfiguracaoFiscalDto>> Salvar([FromBody] UpdateConfiguracaoFiscalDto dto, CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var result = await _service.SalvarAsync(empresaId, dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("focus-nfse/municipio-validacao")]
    public async Task<ActionResult<FocusNfseMunicipioValidacaoDto>> ValidarMunicipioFocusNfse(
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var result = await _service.ValidarMunicipioFocusNfseAsync(empresaId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("focus/webhook-setup")]
    public async Task<ActionResult<FocusWebhookSetupDto>> ObterFocusWebhookSetup(
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var requestBaseUrl = BuildRequestBaseUrl();
        var result = await _service.ObterFocusWebhookSetupAsync(
            empresaId,
            requestBaseUrl,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("focus/webhook-status")]
    public async Task<ActionResult<FocusWebhookSetupDto>> ObterFocusWebhookStatus(
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var result = await _focusWebhookRegistrationService.ObterStatusAsync(
            empresaId,
            BuildRequestBaseUrl(),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("focus/webhook-sync")]
    public async Task<ActionResult<FocusWebhookSetupDto>> SincronizarFocusWebhook(
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();
        var result = await _focusWebhookRegistrationService.SincronizarAsync(
            empresaId,
            BuildRequestBaseUrl(),
            cancellationToken);
        return Ok(result);
    }

    private string BuildRequestBaseUrl()
        => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
}
