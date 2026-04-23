using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Enums;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/documentos-fiscais")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Nivel5")]
public class DocumentosFiscaisController : ApiTenantControllerBase
{
    private readonly INfseService _nfseService;
    private readonly IDfeVendaService _dfeVendaService;
    private readonly IDocumentoFiscalConsultaService _consultaService;

    public DocumentosFiscaisController(
        INfseService nfseService,
        IDfeVendaService dfeVendaService,
        IDocumentoFiscalConsultaService consultaService)
    {
        _nfseService = nfseService;
        _dfeVendaService = dfeVendaService;
        _consultaService = consultaService;
    }

    [HttpPost("nfse/emitir-por-os/{ordemServicoId:guid}")]
    public async Task<ActionResult<DocumentoFiscalDto>> EmitirNfsePorOs(
        Guid ordemServicoId,
        [FromBody] EmitirNfsePorOsDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _nfseService.EmitirPorOrdemServicoAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            ordemServicoId,
            dto,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("nfe/emitir-por-venda/{vendaId:guid}")]
    public async Task<ActionResult<DocumentoFiscalDto>> EmitirNfePorVenda(
        Guid vendaId,
        [FromBody] EmitirDfeVendaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _dfeVendaService.EmitirNfePorVendaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            vendaId,
            dto,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("nfce/emitir-por-venda/{vendaId:guid}")]
    public async Task<ActionResult<DocumentoFiscalDto>> EmitirNfcePorVenda(
        Guid vendaId,
        [FromBody] EmitirDfeVendaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _dfeVendaService.EmitirNfcePorVendaAsync(
            ObterEmpresaId(),
            ObterUsuarioId(),
            vendaId,
            dto,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentoFiscalDto>>> Listar(
        [FromQuery] string? tipoDocumento,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _consultaService.ListarAsync(
            ObterEmpresaId(),
            tipoDocumento,
            status,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentoFiscalDto>> ObterPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _consultaService.ObterPorIdAsync(
            ObterEmpresaId(),
            id,
            cancellationToken);

        if (result is null)
            return NotFound(new { message = "Documento fiscal não encontrado." });

        return Ok(result);
    }

    [HttpPost("{id:guid}/consultar")]
    public async Task<ActionResult<DocumentoFiscalDto>> Consultar(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tipoDocumento = await ObterTipoDocumentoOuFalharAsync(id, cancellationToken);

        var result = tipoDocumento == TipoDocumentoFiscal.Nfse
            ? await _nfseService.ConsultarAsync(ObterEmpresaId(), id, cancellationToken)
            : await _dfeVendaService.ConsultarAsync(ObterEmpresaId(), id, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<DocumentoFiscalDto>> Cancelar(
        Guid id,
        [FromBody] CancelarDocumentoFiscalDto dto,
        CancellationToken cancellationToken)
    {
        var tipoDocumento = await ObterTipoDocumentoOuFalharAsync(id, cancellationToken);

        var result = tipoDocumento == TipoDocumentoFiscal.Nfse
            ? await _nfseService.CancelarAsync(ObterEmpresaId(), ObterUsuarioId(), id, dto, cancellationToken)
            : await _dfeVendaService.CancelarAsync(ObterEmpresaId(), ObterUsuarioId(), id, dto, cancellationToken);

        return Ok(result);
    }

    private async Task<TipoDocumentoFiscal> ObterTipoDocumentoOuFalharAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var tipoDocumento = await _consultaService.ObterTipoDocumentoAsync(
            ObterEmpresaId(),
            id,
            cancellationToken);

        if (!tipoDocumento.HasValue)
            throw new KeyNotFoundException("Documento fiscal não encontrado.");

        return tipoDocumento.Value;
    }
}
