using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.Exceptions;

namespace ServicosApp.API.Controllers;

[Authorize]
public abstract class ApiTenantControllerBase : ControllerBase
{
    protected Guid ObterEmpresaId()
    {
        var claim = User.FindFirst("empresaId")?.Value;

        if (!Guid.TryParse(claim, out var empresaId))
            throw new AppUnauthorizedException("Token sem empresa vinculada.");

        return empresaId;
    }

    protected Guid ObterUsuarioId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(claim, out var usuarioId))
            throw new AppUnauthorizedException("Token sem usuário válido.");

        return usuarioId;
    }

    protected bool EhSuperAdmin()
    {
        return User.FindFirst("isSuperAdmin")?.Value == "true";
    }

    protected string? ObterPerfil()
    {
        return User.FindFirst("perfil")?.Value;
    }
}