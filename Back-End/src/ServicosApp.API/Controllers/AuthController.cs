using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ApiTenantControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _authService = authService;
        _environment = environment;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("registrar-empresa")]
    public async Task<ActionResult<AuthResponseDto>> RegistrarEmpresa(
        [FromBody] RegistrarEmpresaDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegistrarEmpresaAsync(dto, cancellationToken);
        SetAuthCookie(result);
        result.AccessToken = string.Empty;
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        SetAuthCookie(result);
        result.AccessToken = string.Empty;
        return Ok(result);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = CookieSecure(),
            SameSite = CookieSameSite(),
            Path = "/"
        });
        return NoContent();
    }

    private void SetAuthCookie(AuthResponseDto result)
    {
        Response.Cookies.Append("access_token", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = CookieSecure(),
            SameSite = CookieSameSite(),
            Expires = result.ExpiresAtUtc,
            Path = "/"
        });
    }

    private bool CookieSecure() =>
        _configuration.GetValue<bool?>("Cookie:Secure") ?? !_environment.IsDevelopment();

    // Front-end e back-end em domínios diferentes (ex.: Vercel + Render) exigem SameSite=None
    // (e Secure=true, obrigatório junto com None) para o navegador enviar o cookie entre sites.
    private SameSiteMode CookieSameSite() =>
        (_configuration["Cookie:SameSite"] ?? "Strict").Trim().ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "lax" => SameSiteMode.Lax,
            _ => SameSiteMode.Strict
        };
}
