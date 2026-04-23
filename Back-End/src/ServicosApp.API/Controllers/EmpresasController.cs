using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/empresas")]
[Authorize]
public class EmpresasController : ApiTenantControllerBase
{
    private const long MaxLogoBytes = 3 * 1024 * 1024;
    private static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/svg+xml"
    };

    private readonly AppDbContext _context;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly IRemoteImageFetchService _remoteImageFetchService;

    public EmpresasController(
        AppDbContext context,
        IMediaStorageService mediaStorageService,
        IRemoteImageFetchService remoteImageFetchService)
    {
        _context = context;
        _mediaStorageService = mediaStorageService;
        _remoteImageFetchService = remoteImageFetchService;
    }

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Criar([FromBody] CreateEmpresaDto dto, CancellationToken cancellationToken)
    {
        var cnpj = dto.Cnpj.Trim();

        var cnpjJaExiste = await _context.Empresas
            .AnyAsync(x => x.Cnpj == cnpj, cancellationToken);

        if (cnpjJaExiste)
        {
            return Conflict(new ProblemDetails
            {
                Title = "CNPJ ja cadastrado",
                Detail = "Ja existe uma empresa cadastrada com esse CNPJ.",
                Status = StatusCodes.Status409Conflict,
                Instance = HttpContext.Request.Path,
                Type = "https://httpstatuses.com/409"
            });
        }

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = dto.RazaoSocial.Trim(),
            NomeFantasia = dto.NomeFantasia.Trim(),
            Cnpj = cnpj,
            InscricaoEstadual = dto.InscricaoEstadual?.Trim(),
            InscricaoMunicipal = dto.InscricaoMunicipal?.Trim(),
            RegimeTributario = string.IsNullOrWhiteSpace(dto.RegimeTributario)
                ? "SimplesNacional"
                : dto.RegimeTributario.Trim(),
            Email = dto.Email?.Trim(),
            Telefone = dto.Telefone?.Trim(),
            Cep = dto.Cep?.Trim(),
            Logradouro = dto.Logradouro?.Trim(),
            Numero = dto.Numero?.Trim(),
            Complemento = dto.Complemento?.Trim(),
            Bairro = dto.Bairro?.Trim(),
            Cidade = dto.Cidade?.Trim(),
            Uf = dto.Uf?.Trim().ToUpper(),
            LogoUrl = dto.LogoUrl?.Trim(),
            Ativo = true
        };

        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(ObterPorId), new { id = empresa.Id }, empresa);
    }

    [HttpGet]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var empresas = await _context.Empresas
            .AsNoTracking()
            .OrderBy(x => x.NomeFantasia)
            .ToListAsync(cancellationToken);

        return Ok(empresas);
    }

    [HttpGet("minha")]
    public async Task<IActionResult> ObterMinha(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == empresaId, cancellationToken);

        return empresa is null
            ? NotFound(new { message = "Empresa nao encontrada." })
            : Ok(empresa);
    }

    [HttpPost("minha/logo")]
    [Authorize(Policy = "OwnerOuSuperAdmin")]
    [RequestSizeLimit(MaxLogoBytes + 128_000)]
    public async Task<IActionResult> EnviarLogo(
        [FromForm] IFormFile? arquivo,
        [FromForm] string? url,
        CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(x => x.Id == empresaId, cancellationToken);

        if (empresa is null)
            return NotFound(new { message = "Empresa nao encontrada." });

        if ((arquivo is null || arquivo.Length == 0) && string.IsNullOrWhiteSpace(url))
            return BadRequest(new { message = "Selecione uma imagem ou informe uma URL para a logo." });

        var previousLogoUrl = empresa.LogoUrl;
        string fileName;
        string contentType;
        Stream stream;

        try
        {
            if (arquivo is not null && arquivo.Length > 0)
            {
                if (arquivo.Length > MaxLogoBytes)
                    return BadRequest(new { message = "A logo deve ter no maximo 3 MB." });

                if (!AllowedLogoContentTypes.Contains(arquivo.ContentType))
                    return BadRequest(new { message = "Envie uma logo em JPG, PNG, WebP ou SVG." });

                fileName = arquivo.FileName;
                contentType = arquivo.ContentType;
                stream = arquivo.OpenReadStream();
            }
            else
            {
                var remoteImage = await _remoteImageFetchService.DownloadAsync(
                    url ?? string.Empty,
                    MaxLogoBytes,
                    AllowedLogoContentTypes.ToArray(),
                    cancellationToken);

                fileName = remoteImage.FileName;
                contentType = remoteImage.ContentType;
                stream = new MemoryStream(remoteImage.Bytes, writable: false);
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        await using var uploadStream = stream;
        var extension = InlineMediaHelper.ResolveExtension(fileName, contentType);
        var storedLogo = await _mediaStorageService.SaveAsync(
            $"{empresaId}/logo/{Guid.NewGuid():N}{extension}",
            fileName,
            contentType,
            uploadStream,
            cancellationToken);

        empresa.LogoUrl = storedLogo.PublicUrl;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _mediaStorageService.DeleteAsync(previousLogoUrl, cancellationToken);
        }
        catch
        {
        }

        return Ok(empresa);
    }

    [HttpDelete("minha/logo")]
    [Authorize(Policy = "OwnerOuSuperAdmin")]
    public async Task<IActionResult> RemoverLogo(CancellationToken cancellationToken)
    {
        var empresaId = ObterEmpresaId();

        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(x => x.Id == empresaId, cancellationToken);

        if (empresa is null)
            return NotFound(new { message = "Empresa nao encontrada." });

        var previousLogoUrl = empresa.LogoUrl;
        empresa.LogoUrl = null;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _mediaStorageService.DeleteAsync(previousLogoUrl, cancellationToken);
        }
        catch
        {
        }

        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        if (!EhSuperAdmin() && ObterEmpresaId() != id)
            return Forbid();

        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (empresa is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Empresa nao encontrada",
                Detail = $"Nao foi encontrada empresa com id {id}.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path,
                Type = "https://httpstatuses.com/404"
            });
        }

        return Ok(empresa);
    }
}
