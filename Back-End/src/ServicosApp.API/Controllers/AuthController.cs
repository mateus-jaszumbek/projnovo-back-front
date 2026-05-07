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

    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
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
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
        return NoContent();
    }

    private void SetAuthCookie(AuthResponseDto result)
    {
        Response.Cookies.Append("access_token", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = result.ExpiresAtUtc,
            Path = "/"
        });
    }
}
