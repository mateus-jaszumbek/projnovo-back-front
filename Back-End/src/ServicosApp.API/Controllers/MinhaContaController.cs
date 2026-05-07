using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/minha-conta")]
public class MinhaContaController : ApiTenantControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public MinhaContaController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    // LGPD Art. 18 — portabilidade dos dados pessoais
    [HttpGet("dados")]
    public async Task<IActionResult> ExportarDados(CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        var usuario = await _usuarioService.ObterPorIdAsync(
            usuarioId,
            EhSuperAdmin() ? null : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        if (usuario is null)
            return NotFound();

        var export = new
        {
            exportadoEmUtc = DateTime.UtcNow,
            nota = "Exportacao de dados pessoais conforme LGPD Art. 18.",
            dadosPessoais = new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.Ativo,
                usuario.IsSuperAdmin,
            }
        };

        return Ok(export);
    }

    // LGPD Art. 18 — direito de exclusao (inativa a conta, dados removidos em 30 dias)
    [HttpPost("solicitar-exclusao")]
    public async Task<IActionResult> SolicitarExclusao(CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();

        var inativado = await _usuarioService.InativarAsync(
            usuarioId,
            usuarioId,
            EhSuperAdmin() ? null : ObterEmpresaId(),
            EhSuperAdmin(),
            cancellationToken);

        if (!inativado)
            return NotFound();

        return Ok(new
        {
            message = "Solicitacao de exclusao registrada. Sua conta foi inativada e seus dados serao removidos em ate 30 dias conforme LGPD Art. 18."
        });
    }
}
