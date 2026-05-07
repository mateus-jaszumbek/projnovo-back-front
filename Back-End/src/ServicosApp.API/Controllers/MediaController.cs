using Microsoft.AspNetCore.Mvc;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.API.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ApiTenantControllerBase
{
    private readonly IMediaStorageService _mediaStorageService;

    public MediaController(IMediaStorageService mediaStorageService)
    {
        _mediaStorageService = mediaStorageService;
    }

    [HttpGet("{**storageKey}")]
    public async Task<IActionResult> Obter(string storageKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return NotFound();

        if (!EhSuperAdmin())
        {
            var empresaId = ObterEmpresaId();
            var firstSegment = storageKey.Split('/')[0];
            if (!Guid.TryParse(firstSegment, out var segmentEmpresaId) || segmentEmpresaId != empresaId)
                return Forbid();
        }

        await using var file = await _mediaStorageService.OpenReadAsync(storageKey, cancellationToken);
        if (file is null)
            return NotFound();

        using var memory = new MemoryStream();
        await file.Content.CopyToAsync(memory, cancellationToken);

        Response.Headers.CacheControl = "private,max-age=3600";
        return File(memory.ToArray(), file.ContentType);
    }
}
